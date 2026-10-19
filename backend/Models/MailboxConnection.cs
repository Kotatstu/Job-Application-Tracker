namespace backend.Models;

public class MailboxConnection
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = "Gmail";
    public string GoogleEmail { get; set; } = string.Empty;
    public string EncryptedRefreshToken { get; set; } = string.Empty;
    public string? LastHistoryId { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.Now;
}