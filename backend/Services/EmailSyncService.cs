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
    private const int MaxRounds = 5;
    private const int SnippetLength = 300;

    private static readonly string[] CompanySuffixes =
        { "inc", "ltd", "llc", "corp", "co", "company", "jsc", "limited", "corporation" };

    private readonly AppDbContext _context;
    private readonly IGmailReader _reader;
    private readonly IEmailPrefilter _prefilter;
    private readonly IJobEmailClassifier _classifier;
    private readonly ILogger<EmailSyncService> _logger;

    public EmailSyncService(AppDbContext context, IGmailReader reader, IEmailPrefilter prefilter,
        IJobEmailClassifier classifier, ILogger<EmailSyncService> logger)
    {
        _context = context;
        _reader = reader;
        _prefilter = prefilter;
        _classifier = classifier;
        _logger = logger;
    }

    public async Task<SyncSummary> SyncAsync(Guid userId, CancellationToken ct = default)
    {
        var summary = new SyncSummary();
        string? latest = null;
        var complete = false;

        for (var round = 0; round < MaxRounds; round++)
        {
            var read = await _reader.GetNewMessagesAsync(userId, BatchSize, ct);
            summary.ReadStatus = read.Status;
            if (read.Status != GmailReadStatus.Success) return summary;

            latest = read.LatestHistoryId;

            foreach (var email in read.Messages)
            {
                try
                {
                    await ProcessAsync(userId, email, summary, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process message {Id}", email.ExternalMessageId);
                    _context.ChangeTracker.Clear();
                    summary.Failed++;
                }
            }

            if (summary.Failed > 0) return summary;   // keep the checkpoint, retry next time
            if (!read.HasMore) { complete = true; break; }
        }

        if (complete && latest is not null)
        {
            var connection = await _context.MailboxConnections
                .FirstAsync(m => m.UserId == userId && m.Provider == "Gmail", ct);
            connection.LastHistoryId = latest;
            connection.LastSyncedAt = DateTime.Now;
            await _context.SaveChangesAsync(ct);
            summary.CheckpointAdvanced = true;
        }

        return summary;
    }

    private async Task ProcessAsync(Guid userId, GmailMessageDto email, SyncSummary summary, CancellationToken ct)
    {
        var row = await _context.EmailMessages
            .FirstOrDefaultAsync(e => e.UserId == userId && e.ExternalMessageId == email.ExternalMessageId, ct);

        // 1. Prefilter: content-free marker only
        var pre = _prefilter.Evaluate(email);
        if (pre.Skip)
        {
            row ??= NewRow(userId, email);
            row.Subject = "";
            row.SenderEmail = "";
            row.Snippet = null;
            row.ProcessingStatus = EmailProcessingStatus.Skipped;
            await _context.SaveChangesAsync(ct);
            summary.Skipped++;
            return;
        }

        // 2. Classify
        var result = await _classifier.ClassifyAsync(email.Subject, email.BodyText, ct);
        row ??= NewRow(userId, email);

        if (result is null || !result.IsJobRelated || result.Kind == "other")
        {
            row.Snippet = null;
            row.ProcessingStatus = EmailProcessingStatus.NotJobRelated;
            await _context.SaveChangesAsync(ct);
            summary.NotJobRelated++;
            return;
        }

        row.Snippet = Truncate(email.BodyText, SnippetLength);

        // 3. Match
        var normalized = Normalize(result.CompanyName);
        Company? company = null;
        if (normalized.Length > 0)
        {
            var companies = await _context.Companies
                .Where(c => c.UserId == userId)
                .ToListAsync(ct);
            company = companies.FirstOrDefault(c => Normalize(c.CompanyName) == normalized);
        }

        var apps = company is null
            ? new List<JobApplication>()
            : await _context.JobApplications
                .Where(a => a.UserId == userId && a.CompanyId == company.Id)
                .ToListAsync(ct);

        var status = MapStatus(result.Kind);

        // 4. Stage
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
            var best = apps.Count == 1 ? apps[0] : PickBestByTitle(apps, result.JobTitle);
            _context.PendingStatusChanges.Add(new PendingStatusChange
            {
                UserId = userId,
                EmailMessage = row,
                SuggestedJobApplicationId = best.Id,
                StatusGuess = status
            });
            row.ProcessingStatus = EmailProcessingStatus.Staged;
            summary.StatusChanges++;
        }

        await _context.SaveChangesAsync(ct);
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

    private static JobApplication PickBestByTitle(List<JobApplication> apps, string? title)
    {
        var wanted = Tokens(title);
        return apps
            .OrderByDescending(a => Tokens(a.JobTitle).Intersect(wanted).Count())
            .First();
    }

    private static HashSet<string> Tokens(string? text) =>
        Regex.Split((text ?? "").ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(t => t.Length > 0).ToHashSet();

    private static string Normalize(string? name)
    {
        var words = Regex.Split((name ?? "").ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(w => w.Length > 0 && !CompanySuffixes.Contains(w));
        return string.Concat(words);
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max];
}