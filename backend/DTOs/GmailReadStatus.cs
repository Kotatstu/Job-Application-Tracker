using backend.DTOs;

namespace backend.Services;

public enum GmailReadStatus
{
    Success,
    NotConnected,
    NoCheckpoint,
    ReconnectRequired,
    CheckpointExpired
}

public class GmailReadResult
{
    public GmailReadStatus Status { get; set; }
    public List<GmailMessageDto> Messages { get; set; } = new();
    public string? StartHistoryId { get; set; }
    public string? LatestHistoryId { get; set; }
    public int TotalNewMessages { get; set; }
    public int AlreadyProcessed { get; set; }
    public bool HasMore { get; set; }
}