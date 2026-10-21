using backend.Services;

namespace backend.Interfaces;

public interface IGmailReader
{
    Task<GmailReadResult> GetNewMessagesAsync(Guid userId, int max, CancellationToken ct = default);
}