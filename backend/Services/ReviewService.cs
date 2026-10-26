using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class ReviewService : IReviewService
{
    private const string Pending = "Pending";
    private const string Confirmed = "Confirmed";
    private const string Dismissed = "Dismissed";

    private readonly AppDbContext _context;
    private readonly IJobApplicationService _applications;

    public ReviewService(AppDbContext context, IJobApplicationService applications)
    {
        _context = context;
        _applications = applications;
    }

    // ---------- Summary ----------

    public async Task<ReviewSummaryDTO> GetSummaryAsync(Guid userId)
    {
        var apps = await _context.PendingJobApplications
            .CountAsync(p => p.UserId == userId && p.ReviewStatus == Pending);
        var changes = await _context.PendingStatusChanges
            .CountAsync(p => p.UserId == userId && p.ReviewStatus == Pending);

        return new ReviewSummaryDTO { PendingApplications = apps, PendingStatusChanges = changes };
    }

    // ---------- Pending new applications ----------

    public async Task<List<PendingApplicationDTO>> GetApplicationsAsync(Guid userId, string reviewStatus)
    {
        return await _context.PendingJobApplications.AsNoTracking()
            .Where(p => p.UserId == userId && p.ReviewStatus == reviewStatus)
            .OrderByDescending(p => p.EmailMessage.ReceivedAt)
            .Select(p => new PendingApplicationDTO
            {
                Id = p.Id,
                CompanyNameGuess = p.CompanyNameGuess,
                JobTitleGuess = p.JobTitleGuess,
                StatusGuess = p.StatusGuess,
                ReviewStatus = p.ReviewStatus,
                DetectedAt = p.DetectedAt,
                ReviewedAt = p.ReviewedAt,
                LinkedJobApplicationId = p.LinkedJobApplicationId,
                Email = new EmailContextDTO
                {
                    EmailMessageId = p.EmailMessageId,
                    Subject = p.EmailMessage.Subject,
                    SenderEmail = p.EmailMessage.SenderEmail,
                    ReceivedAt = p.EmailMessage.ReceivedAt,
                    Snippet = p.EmailMessage.Snippet
                }
            })
            .ToListAsync();
    }

    public async Task<(JobApplicationResponseDTO?, ReviewResult)> ConfirmApplicationAsync(
        int id, ConfirmPendingApplicationDTO dto, Guid userId)
    {
        var pending = await _context.PendingJobApplications
            .Include(p => p.EmailMessage)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (pending is null) return (null, ReviewResult.NotFound);
        if (pending.ReviewStatus != Pending) return (null, ReviewResult.AlreadyReviewed);

        var email = pending.EmailMessage;
        var status = (dto.Status ?? pending.StatusGuess ?? "Applied").Trim();
        if (status.Length == 0) status = "Applied";

        var appliedAt = dto.DateApplied ?? email.ReceivedAt;

        // One transaction: company is already chosen, so application + history + status change + pending update
        // either all happen or none do. (Leaving without Commit rolls everything back.)
        await using var tx = await _context.Database.BeginTransactionAsync();

        var (created, createResult) = await _applications.CreateAsync(
            new JobApplicationCreateDTO
            {
                CompanyId = dto.CompanyId,
                JobTitle = dto.JobTitle.Trim(),
                JobPostingUrl = dto.JobPostingUrl,
                Location = dto.Location,
                DateApplied = appliedAt,
                Notes = dto.Notes
            },
            userId, source: "Email", sourceEmailId: email.Id, changedAt: appliedAt);

        if (created is null) return (null, ReviewResult.CompanyNotFound);

        var result = created;

        // Every application starts as "Applied"; anything else is a second step linked to the email
        if (!string.Equals(status, "Applied", StringComparison.OrdinalIgnoreCase))
        {
            var statusAt = email.ReceivedAt < appliedAt ? appliedAt : email.ReceivedAt;

            var (updated, updateResult) = await _applications.UpdateStatusAsync(
                new ChangeStatusDTO { Status = status },
                created.Id, userId, source: "Email", sourceEmailId: email.Id, changedAt: statusAt);

            if (updated is null) return (null, ReviewResult.InvalidStatus);
            result = updated;
        }

        pending.ReviewStatus = Confirmed;
        pending.ReviewedAt = DateTime.Now;
        pending.LinkedJobApplicationId = created.Id;

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return (result, ReviewResult.Success);
    }

    public async Task<ReviewResult> DismissApplicationAsync(int id, Guid userId)
    {
        var pending = await _context.PendingJobApplications
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (pending is null) return ReviewResult.NotFound;
        if (pending.ReviewStatus != Pending) return ReviewResult.AlreadyReviewed;

        pending.ReviewStatus = Dismissed;
        pending.ReviewedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return ReviewResult.Success;
    }

    // ---------- Pending status changes ----------

    public async Task<List<PendingStatusChangeDTO>> GetStatusChangesAsync(Guid userId, string reviewStatus)
    {
        // PendingStatusChange has no navigation to the application, so it is a left join by id
        var query =
            from p in _context.PendingStatusChanges.AsNoTracking()
            where p.UserId == userId && p.ReviewStatus == reviewStatus
            join a in _context.JobApplications.Where(x => x.UserId == userId)
                on p.SuggestedJobApplicationId equals (int?)a.Id into matches
            from a in matches.DefaultIfEmpty()
            orderby p.EmailMessage.ReceivedAt descending
            select new PendingStatusChangeDTO
            {
                Id = p.Id,
                StatusGuess = p.StatusGuess,
                NoteGuess = p.NoteGuess,
                ReviewStatus = p.ReviewStatus,
                DetectedAt = p.DetectedAt,
                ReviewedAt = p.ReviewedAt,
                SuggestedApplication = a == null ? null : new SuggestedApplicationDTO
                {
                    Id = a.Id,
                    CompanyName = a.Company.CompanyName,
                    JobTitle = a.JobTitle,
                    CurrentStatus = a.CurrentStatus
                },
                Email = new EmailContextDTO
                {
                    EmailMessageId = p.EmailMessageId,
                    Subject = p.EmailMessage.Subject,
                    SenderEmail = p.EmailMessage.SenderEmail,
                    ReceivedAt = p.EmailMessage.ReceivedAt,
                    Snippet = p.EmailMessage.Snippet
                }
            };

        return await query.ToListAsync();
    }

    public async Task<(JobApplicationResponseDTO?, ReviewResult)> ConfirmStatusChangeAsync(
        int id, ConfirmStatusChangeDTO dto, Guid userId)
    {
        var pending = await _context.PendingStatusChanges
            .Include(p => p.EmailMessage)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (pending is null) return (null, ReviewResult.NotFound);
        if (pending.ReviewStatus != Pending) return (null, ReviewResult.AlreadyReviewed);

        var email = pending.EmailMessage;

        await using var tx = await _context.Database.BeginTransactionAsync();

        var (updated, updateResult) = await _applications.UpdateStatusAsync(
            new ChangeStatusDTO { Status = dto.Status.Trim(), Note = dto.Note ?? pending.NoteGuess },
            dto.JobApplicationId, userId,
            source: "Email", sourceEmailId: email.Id, changedAt: email.ReceivedAt);

        if (updated is null)
            return (null, updateResult == UpdateStatusResult.NotFound
                ? ReviewResult.ApplicationNotFound
                : ReviewResult.InvalidStatus);

        pending.ReviewStatus = Confirmed;
        pending.ReviewedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return (updated, ReviewResult.Success);
    }

    public async Task<ReviewResult> DismissStatusChangeAsync(int id, Guid userId)
    {
        var pending = await _context.PendingStatusChanges
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (pending is null) return ReviewResult.NotFound;
        if (pending.ReviewStatus != Pending) return ReviewResult.AlreadyReviewed;

        pending.ReviewStatus = Dismissed;
        pending.ReviewedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return ReviewResult.Success;
    }
}