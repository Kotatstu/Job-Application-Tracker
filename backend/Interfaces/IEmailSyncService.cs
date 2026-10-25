namespace backend.Interfaces;

using backend.Services;

public class SyncSummary
{
    public GmailReadStatus ReadStatus { get; set; }
    public int Read { get; set; }              // emails fetched this run
    public int AlreadyDone { get; set; }       // dropped because they were handled before
    public int NotJobRelated { get; set; }
    public int JobRelated { get; set; }        // NewApplications + StatusChanges + NoAction
    public int NewApplications { get; set; }   // staged as PendingJobApplications
    public int StatusChanges { get; set; }     // staged as PendingStatusChanges
    public int NoAction { get; set; }          // confirmation for an application already tracked
    public int Failed { get; set; }            // classifier gave no answer, or saving threw
    public bool CheckpointAdvanced { get; set; }   // true when everything waiting was handled and nothing failed
}

public interface IEmailSyncService
{
    Task<SyncSummary> SyncAsync(Guid userId, CancellationToken ct = default);
}