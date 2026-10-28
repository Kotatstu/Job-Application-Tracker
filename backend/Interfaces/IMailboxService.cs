using backend.DTOs;

namespace backend.Interfaces;

public interface IMailboxService
{
    Task<MailboxStatusDTO> GetStatusAsync(Guid userId);
    Task<(SyncRequestResult Result, int RetryAfterSeconds)> RequestSyncAsync(Guid userId);
    Task<List<Guid>> GetConnectedUserIdsAsync();
}