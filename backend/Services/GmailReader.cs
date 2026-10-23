using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class GmailReader : IGmailReader
{
    private readonly GoogleOptions _options;
    private readonly AppDbContext _context;
    private readonly ITokenProtector _protector;
    private readonly ILogger<GmailReader> _logger;

    public GmailReader(
        IOptions<GoogleOptions> options,
        AppDbContext context,
        ITokenProtector protector,
        ILogger<GmailReader> logger)
    {
        _options = options.Value;
        _context = context;
        _protector = protector;
        _logger = logger;
    }

    public async Task<GmailReadResult> GetNewMessagesAsync(Guid userId, int max, CancellationToken ct = default)
    {
        var connection = await _context.MailboxConnections.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Provider == "Gmail", ct);

        if (connection is null)
            return new GmailReadResult { Status = GmailReadStatus.NotConnected };

        if (!ulong.TryParse(connection.LastHistoryId, out var startId))
            return new GmailReadResult { Status = GmailReadStatus.NoCheckpoint };

        string refreshToken;
        try
        {
            refreshToken = _protector.Unprotect(connection.EncryptedRefreshToken);
        }
        catch (CryptographicException)
        {
            return new GmailReadResult { Status = GmailReadStatus.ReconnectRequired };
        }

        using var flow = GoogleFlowFactory.Create(_options);
        var credential = new UserCredential(flow, userId.ToString(),
            new TokenResponse { RefreshToken = refreshToken });

        using var gmail = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Job Tracker"
        });

        try
        {
            List<string> ids;
            string? latest;
            try
            {
                (ids, latest) = await FindNewMessageIdsAsync(gmail, startId, ct);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return new GmailReadResult { Status = GmailReadStatus.CheckpointExpired };
            }

            var done = (await _context.EmailMessages.AsNoTracking()
                    .Where(e => e.UserId == userId
                            && ids.Contains(e.ExternalMessageId)
                            && e.ProcessingStatus != EmailProcessingStatus.Pending)
                    .Select(e => e.ExternalMessageId)
                    .ToListAsync(ct))
                .ToHashSet();

            var toFetch = ids.Where(id => !done.Contains(id)).ToList();

            var result = new GmailReadResult
            {
                Status = GmailReadStatus.Success,
                StartHistoryId = startId.ToString(),
                LatestHistoryId = latest,
                TotalNewMessages = ids.Count,
                AlreadyProcessed = done.Count,
                HasMore = toFetch.Count > max
            };


            foreach (var id in ids.Take(max))
            {
                try
                {
                    result.Messages.Add(await FetchAndCleanAsync(gmail, id, ct));
                }
                catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Message {Id} vanished before it could be fetched", id);
                }
            }

            _logger.LogInformation("Gmail read: {New} new, {Returned} returned", ids.Count, result.Messages.Count);
            return result;
        }
        catch (TokenResponseException)
        {
            return new GmailReadResult { Status = GmailReadStatus.ReconnectRequired };
        }
    }

    private static async Task<(List<string> Ids, string? Latest)> FindNewMessageIdsAsync(
        GmailService gmail, ulong startId, CancellationToken ct)
    {
        var ids = new List<string>();
        var seen = new HashSet<string>();
        string? latest = null;
        string? pageToken = null;

        do
        {
            var request = gmail.Users.History.List("me");
            request.StartHistoryId = startId;
            request.LabelId = "INBOX";
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(ct);
            latest = response.HistoryId?.ToString();

            if (response.History != null)
                foreach (var record in response.History)
                    if (record.MessagesAdded != null)
                        foreach (var added in record.MessagesAdded)
                            if (added.Message?.Id != null && seen.Add(added.Message.Id))
                                ids.Add(added.Message.Id);

            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return (ids, latest);
    }

    private static async Task<GmailMessageDto> FetchAndCleanAsync(
        GmailService gmail, string id, CancellationToken ct)
    {
        var message = await gmail.Users.Messages.Get("me", id).ExecuteAsync(ct);
        var headers = message.Payload?.Headers ?? new List<MessagePartHeader>();

        string Header(string name) =>
            headers.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))?.Value
            ?? string.Empty;

        return new GmailMessageDto
        {
            ExternalMessageId = id,
            Subject = Header("Subject"),
            SenderEmail = ParseAddress(Header("From")),
            ReceivedAt = message.InternalDate.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value).LocalDateTime
                : DateTime.Now,
            BodyText = EmailTextCleaner.Clean(ExtractText(message.Payload)),
            LabelIds = message.LabelIds?.ToList() ?? new List<string>(),
            HasListUnsubscribe = !string.IsNullOrEmpty(Header("List-Unsubscribe")),
            Precedence = Header("Precedence"),
        };
    }

    private static string ExtractText(MessagePart? payload)
    {
        if (payload is null) return string.Empty;

        var plain = FindBodyData(payload, "text/plain");
        if (plain is not null) return DecodeBase64Url(plain);

        var html = FindBodyData(payload, "text/html");
        return html is not null ? HtmlToText(DecodeBase64Url(html)) : string.Empty;
    }

    private static string? FindBodyData(MessagePart part, string mimeType)
    {
        if (part.MimeType == mimeType && !string.IsNullOrEmpty(part.Body?.Data))
            return part.Body.Data;

        if (part.Parts != null)
            foreach (var child in part.Parts)
            {
                var found = FindBodyData(child, mimeType);
                if (found is not null) return found;
            }

        return null;
    }

    private static string DecodeBase64Url(string data)
    {
        var s = data.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }

    private static string HtmlToText(string html)
    {
        var noScripts = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", " ",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var withBreaks = Regex.Replace(noScripts, @"<(br|/p|/div|/tr|/li)[^>]*>", "\n",
            RegexOptions.IgnoreCase);
        var stripped = Regex.Replace(withBreaks, @"<[^>]+>", " ");
        return WebUtility.HtmlDecode(stripped);
    }

    private static string ParseAddress(string from)
    {
        try
        {
            return new MailAddress(from).Address;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return from;
        }
    }
}