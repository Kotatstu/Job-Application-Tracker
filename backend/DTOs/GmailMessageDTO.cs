namespace backend.DTOs;

public class GmailMessageDto
{
    public string ExternalMessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public string BodyText { get; set; } = string.Empty;
    public List<string> LabelIds { get; set; } = new();
    public bool HasListUnsubscribe { get; set; }
    public string Precedence { get; set; } = string.Empty;
}