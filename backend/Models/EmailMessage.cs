namespace backend.Models;

public class EmailMessage
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string ExternalMessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string? Snippet { get; set; }
    public string? Kind { get; set; }
    public bool IsJobRelated { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}