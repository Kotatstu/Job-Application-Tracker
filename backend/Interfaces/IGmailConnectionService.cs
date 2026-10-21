using backend.DTOs;

namespace backend.Interfaces;

public interface IGmailConnectionService
{
    string BuildAuthorizationUrl(Guid userId);
    Task<GmailConnectResult> CompleteConnectionAsync(string code, string state, CancellationToken ct = default);
}