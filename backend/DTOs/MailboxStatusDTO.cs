using backend.Interfaces;

namespace backend.DTOs;

public class MailboxStatusDTO
{
    public bool Connected { get; set; }
    public string? GoogleEmail { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }          // from the database: last time the checkpoint moved

    public string SyncState { get; set; } = "Idle";      // Idle | Queued | Running
    public DateTime? RunStartedAt { get; set; }
    public DateTime? LastRunFinishedAt { get; set; }     // in memory: null after an app restart
    public string? LastReadStatus { get; set; }          // Success, ReconnectRequired, CheckpointExpired, ...
    public SyncSummary? LastSummary { get; set; }
    public string? LastError { get; set; }

    public bool NeedsReconnect { get; set; }
    public int CooldownRemainingSeconds { get; set; }
}