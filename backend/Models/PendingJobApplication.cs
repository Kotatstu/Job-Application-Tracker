namespace backend.Models;

public class PendingJobApplication
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int EmailMessageId { get; set; }
    public string CompanyNameGuess { get; set; } = string.Empty;
    public string? JobTitleGuess { get; set; }
    public string? StatusGuess { get; set; }
    public string ReviewStatus { get; set; } = "Pending";
    public DateTime DetectedAt { get; set; } = DateTime.Now;
    public DateTime? ReviewedAt { get; set; }
    public int? LinkedJobApplicationId { get; set; }

    public EmailMessage EmailMessage { get; set; } = null!;
}