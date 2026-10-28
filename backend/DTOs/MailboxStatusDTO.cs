using backend.Interfaces;

namespace backend.DTOs;

public class MailboxStatusDTO
{
    public bool Connected { get; set; }
    public string? GoogleEmail { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; } 

    public string SyncState { get; set; } = "Idle";    
    public DateTime? RunStartedAt { get; set; }
    public DateTime? LastRunFinishedAt { get; set; }  
    public string? LastReadStatus { get; set; }   
    public SyncSummary? LastSummary { get; set; }
    public string? LastError { get; set; }

    public bool NeedsReconnect { get; set; }
    public int CooldownRemainingSeconds { get; set; }
}