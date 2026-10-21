using System.Security.Cryptography;
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class GmailConnectionService : IGmailConnectionService
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    private readonly GoogleOptions _options;
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _context;
    private readonly ITokenProtector _protector;

    public GmailConnectionService(
        IOptions<GoogleOptions> options,
        IMemoryCache cache,
        AppDbContext context,
        ITokenProtector protector)
    {
        _options = options.Value;
        _cache = cache;
        _context = context;
        _protector = protector;
    }

    public string BuildAuthorizationUrl(Guid userId)
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _cache.Set(StateKey(state), userId, StateLifetime);

        using var flow = CreateFlow();
        var request = flow.CreateAuthorizationCodeRequest(_options.RedirectUri);
        request.State = state;

        if (request is GoogleAuthorizationCodeRequestUrl googleRequest)
        {
            googleRequest.AccessType = "offline";
        }

        return request.Build().AbsoluteUri;
    }

    public async Task<GmailConnectResult> CompleteConnectionAsync(string code, string state, CancellationToken ct = default)
    {
        if (!_cache.TryGetValue(StateKey(state), out Guid userId))
            return GmailConnectResult.InvalidState;

        _cache.Remove(StateKey(state)); // single use

        using var flow = CreateFlow();

        TokenResponse token;
        try
        {
            token = await flow.ExchangeCodeForTokenAsync(userId.ToString(), code, _options.RedirectUri, ct);
        }
        catch (TokenResponseException)
        {
            return GmailConnectResult.GoogleError;
        }

        if (string.IsNullOrEmpty(token.RefreshToken))
            return GmailConnectResult.NoRefreshToken;

        var credential = new UserCredential(flow, userId.ToString(), token);
        using var gmail = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Job Tracker"
        });

        var profile = await gmail.Users.GetProfile("me").ExecuteAsync(ct);

        var connection = await _context.MailboxConnections
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Provider == "Gmail", ct);

        if (connection is null)
        {
            connection = new MailboxConnection { UserId = userId, Provider = "Gmail" };
            _context.MailboxConnections.Add(connection);
        }

        connection.GoogleEmail = profile.EmailAddress;
        connection.EncryptedRefreshToken = _protector.Protect(token.RefreshToken);
        connection.LastHistoryId = profile.HistoryId?.ToString();
        connection.ConnectedAt = DateTime.Now;

        await _context.SaveChangesAsync(ct);
        return GmailConnectResult.Success;
    }

    private GoogleAuthorizationCodeFlow CreateFlow() => GoogleFlowFactory.Create(_options);
    private static string StateKey(string state) => $"gmail-connect-state:{state}";
}