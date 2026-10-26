using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class ReviewSummaryDTO
{
    public int PendingApplications { get; set; }
    public int PendingStatusChanges { get; set; }
}

public class EmailContextDTO
{
    public int EmailMessageId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string? Snippet { get; set; }
}

public class PendingApplicationDTO
{
    public int Id { get; set; }
    public string CompanyNameGuess { get; set; } = string.Empty;
    public string? JobTitleGuess { get; set; }
    public string? StatusGuess { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? LinkedJobApplicationId { get; set; }
    public EmailContextDTO Email { get; set; } = new();
}

public class SuggestedApplicationDTO
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
}

public class PendingStatusChangeDTO
{
    public int Id { get; set; }
    public SuggestedApplicationDTO? SuggestedApplication { get; set; }   // null: the user must pick one
    public string StatusGuess { get; set; } = string.Empty;
    public string? NoteGuess { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public EmailContextDTO Email { get; set; } = new();
}

// What the user submits after editing a suggested new application
public class ConfirmPendingApplicationDTO
{
    [Range(1, int.MaxValue)]
    public int CompanyId { get; set; }          // picked from the company picker (or created there first)

    [Required]
    public string JobTitle { get; set; } = string.Empty;

    public DateTime? DateApplied { get; set; }  // null: use the email's received time
    public string? Status { get; set; }         // null: use the guess, or "Applied"
    public string? JobPostingUrl { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class ConfirmStatusChangeDTO
{
    [Range(1, int.MaxValue)]
    public int JobApplicationId { get; set; }   // the suggestion, or the one the user picked instead

    [Required]
    public string Status { get; set; } = string.Empty;

    public string? Note { get; set; }           // null: use the guessed note
}