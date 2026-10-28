using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class EmailSyncService : IEmailSyncService
{
    private const int BatchSize = 25;
    private const int SnippetLength = 300;

    //words dropped from company names before comparing ("Acme Inc" matches "Acme").
    private static readonly string[] CompanySuffixes =
        { "inc", "ltd", "llc", "corp", "co", "company", "jsc", "limited", "corporation", "tnhh", "cp" };

    private readonly AppDbContext _context;
    private readonly IGmailReader _reader;
    private readonly IJobEmailClassifier _classifier;
    private readonly ILogger<EmailSyncService> _logger;

    public EmailSyncService(AppDbContext context, IGmailReader reader,
        IJobEmailClassifier classifier, ILogger<EmailSyncService> logger)
    {
        _context = context;
        _reader = reader;
        _classifier = classifier;
        _logger = logger;
    }

    public async Task<SyncSummary> SyncAsync(Guid userId, CancellationToken ct = default)
    {
        var summary = new SyncSummary();

        var read = await _reader.GetNewMessagesAsync(userId, BatchSize, ct);
        summary.ReadStatus = read.Status;
        if (read.Status != GmailReadStatus.Success) return summary;

        summary.Read = read.Messages.Count;
        summary.AlreadyDone = read.AlreadyProcessed;

        foreach (var email in read.Messages)
        {
            try
            {
                await ProcessAsync(userId, email, summary, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process message {Id}", email.ExternalMessageId);
                _context.ChangeTracker.Clear();   // drop the half-built rows so the next email starts clean
                summary.Failed++;
            }
        }

        if (!read.HasMore && summary.Failed == 0 && read.LatestHistoryId is not null)
        {
            var connection = await _context.MailboxConnections
                .FirstOrDefaultAsync(m => m.UserId == userId && m.Provider == "Gmail", ct);

            if (connection is not null)
            {
                connection.LastHistoryId = read.LatestHistoryId;
                connection.LastSyncedAt = DateTime.Now;
                await _context.SaveChangesAsync(ct);
                summary.CheckpointAdvanced = true;
            }
        }

        return summary;
    }

    private async Task ProcessAsync(Guid userId, GmailMessageDto email, SyncSummary summary, CancellationToken ct)
    {
        var row = await _context.EmailMessages
            .FirstOrDefaultAsync(e => e.UserId == userId && e.ExternalMessageId == email.ExternalMessageId, ct);

        if (row is not null && row.ProcessingStatus != EmailProcessingStatus.Pending)
        {
            summary.AlreadyDone++;
            return;
        }

        var result = await _classifier.ClassifyAsync(email.Subject, email.BodyText, ct);
        if (result is null)
        {
            _logger.LogWarning("Classifier returned no result for message {Id}", email.ExternalMessageId);
            summary.Failed++;
            return;
        }

        row ??= NewRow(userId, email);

        var jobRelated = result.IsJobRelated && result.Kind != "other";
        row.Kind = result.Kind;
        row.IsJobRelated = jobRelated;

        if (!jobRelated)
        {
            row.Snippet = null;
            row.ProcessingStatus = EmailProcessingStatus.NotJobRelated;
            row.ProcessedAt = DateTime.Now;
            summary.NotJobRelated++;
            await _context.SaveChangesAsync(ct);
            return;
        }

        row.Snippet = Truncate(email.BodyText, SnippetLength);
        summary.JobRelated++;

        var apps = await FindApplicationsAsync(userId, result.CompanyName, ct);
        var status = MapStatus(result.Kind);

        if (apps.Count == 0)
        {
            _context.PendingJobApplications.Add(new PendingJobApplication
            {
                UserId = userId,
                EmailMessage = row,
                CompanyNameGuess = result.CompanyName ?? "Unknown",
                JobTitleGuess = result.JobTitle,
                StatusGuess = status
            });
            row.ProcessingStatus = EmailProcessingStatus.Staged;
            summary.NewApplications++;
        }
        else if (result.Kind == "application_received")
        {
            row.ProcessingStatus = EmailProcessingStatus.NoAction;
            summary.NoAction++;
        }
        else
        {
            _context.PendingStatusChanges.Add(new PendingStatusChange
            {
                UserId = userId,
                EmailMessage = row,
                SuggestedJobApplicationId = PickApplicationId(apps, result.JobTitle),
                StatusGuess = status
            });
            row.ProcessingStatus = EmailProcessingStatus.Staged;
            summary.StatusChanges++;
        }

        row.ProcessedAt = DateTime.Now;
        await _context.SaveChangesAsync(ct);
    }

    private async Task<List<JobApplication>> FindApplicationsAsync(Guid userId, string? companyName, CancellationToken ct)
    {
        var wanted = Normalize(companyName);
        if (wanted.Length == 0) return new List<JobApplication>();

        var companies = await _context.Companies
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

        var ids = companies
            .Where(c => Normalize(c.CompanyName) == wanted)
            .Select(c => c.Id)
            .ToList();

        if (ids.Count == 0) return new List<JobApplication>();

        return await _context.JobApplications
            .Where(a => a.UserId == userId && ids.Contains(a.CompanyId))
            .ToListAsync(ct);
    }

    private static int? PickApplicationId(List<JobApplication> apps, string? title)
    {
        if (apps.Count == 1) return apps[0].Id;

        var wanted = Tokens(title);
        var best = apps
            .Select(a => new { a.Id, Score = Tokens(a.JobTitle).Intersect(wanted).Count() })
            .OrderByDescending(x => x.Score)
            .First();

        return best.Score > 0 ? best.Id : null;
    }

    private EmailMessage NewRow(Guid userId, GmailMessageDto email)
    {
        var row = new EmailMessage
        {
            UserId = userId,
            ExternalMessageId = email.ExternalMessageId,
            Subject = email.Subject,
            SenderEmail = email.SenderEmail,
            ReceivedAt = email.ReceivedAt,
            ProcessingStatus = EmailProcessingStatus.Pending
        };
        _context.EmailMessages.Add(row);
        return row;
    }

    private static string MapStatus(string kind) => kind switch
    {
        "interview_invite" => "Interview",
        "offer" => "Offer",
        "rejection" => "Rejected",
        _ => "Applied"
    };

    private static string Fold(string? text)
    {
        var decomposed = (text ?? "").ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static HashSet<string> Tokens(string? text) =>
        Regex.Split(Fold(text), @"[^\p{L}\p{N}]+")
            .Where(t => t.Length > 0).ToHashSet();

    private static string Normalize(string? name) =>
        string.Concat(Regex.Split(Fold(name), @"[^\p{L}\p{N}]+")
            .Where(w => w.Length > 0 && !CompanySuffixes.Contains(w)));

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max];
}