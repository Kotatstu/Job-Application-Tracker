namespace backend.Models;

public class PendingStatusChange
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int EmailMessageId { get; set; }
    public int? SuggestedJobApplicationId { get; set; }
    public string StatusGuess { get; set; } = string.Empty;
    public string? NoteGuess { get; set; }
    public string ReviewStatus { get; set; } = "Pending";
    public DateTime DetectedAt { get; set; } = DateTime.Now;
    public DateTime? ReviewedAt { get; set; }

    public EmailMessage EmailMessage { get; set; } = null!;
}