namespace backend.Interfaces;

using backend.Services;

public class SyncSummary
{
    public GmailReadStatus ReadStatus { get; set; }
    public int Skipped { get; set; }
    public int NotJobRelated { get; set; }
    public int NewApplications { get; set; }
    public int StatusChanges { get; set; }
    public int NoAction { get; set; }
    public int Failed { get; set; }
    public bool CheckpointAdvanced { get; set; }
}

public interface IEmailSyncService
{
    Task<SyncSummary> SyncAsync(Guid userId, CancellationToken ct = default);
}