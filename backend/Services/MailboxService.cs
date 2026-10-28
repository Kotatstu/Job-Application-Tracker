using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class MailboxService : IMailboxService
{
    private readonly AppDbContext _context;
    private readonly SyncCoordinator _coordinator;

    public MailboxService(AppDbContext context, SyncCoordinator coordinator)
    {
        _context = context;
        _coordinator = coordinator;
    }

    public async Task<MailboxStatusDTO> GetStatusAsync(Guid userId)
    {
        var connection = await _context.MailboxConnections.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Provider == "Gmail");

        var snap = _coordinator.GetSnapshot(userId);

        var dto = new MailboxStatusDTO
        {
            Connected = connection is not null,
            GoogleEmail = connection?.GoogleEmail,
            ConnectedAt = connection?.ConnectedAt,
            LastSyncedAt = connection?.LastSyncedAt,
            SyncState = snap.State.ToString(),
            RunStartedAt = snap.State == SyncRunState.Idle ? null : snap.StartedAt,
            LastRunFinishedAt = snap.FinishedAt,
            LastReadStatus = snap.LastSummary?.ReadStatus.ToString(),
            LastSummary = snap.LastSummary,
            LastError = snap.LastError,
            CooldownRemainingSeconds = snap.CooldownRemainingSeconds
        };

        if (connection is not null && snap.LastSummary is { } summary && snap.FinishedAt is { } finished
            && finished > connection.ConnectedAt)
        {
            dto.NeedsReconnect = summary.ReadStatus is
                GmailReadStatus.ReconnectRequired or
                GmailReadStatus.CheckpointExpired or
                GmailReadStatus.NoCheckpoint;
        }

        return dto;
    }

    public async Task<(SyncRequestResult Result, int RetryAfterSeconds)> RequestSyncAsync(Guid userId)
    {
        var connected = await _context.MailboxConnections
            .AnyAsync(m => m.UserId == userId && m.Provider == "Gmail");

        if (!connected) return (SyncRequestResult.NotConnected, 0);

        return _coordinator.TryRequest(userId);
    }

    public async Task<List<Guid>> GetConnectedUserIdsAsync()
    {
        return await _context.MailboxConnections.AsNoTracking()
            .Where(m => m.Provider == "Gmail")
            .Select(m => m.UserId)
            .ToListAsync();
    }
}