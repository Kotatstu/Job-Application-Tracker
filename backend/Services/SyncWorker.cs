using backend.DTOs;
using backend.Interfaces;

namespace backend.Services;

public class SyncWorker : BackgroundService
{
    private readonly SyncCoordinator _coordinator;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SyncWorker> _logger;

    public SyncWorker(SyncCoordinator coordinator, IServiceScopeFactory scopes, ILogger<SyncWorker> logger)
    {
        _coordinator = coordinator;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var userId in _coordinator.Reader.ReadAllAsync(stoppingToken))
            {
                _coordinator.MarkStarted(userId);

                try
                {
                    using var scope = _scopes.CreateScope();
                    var sync = scope.ServiceProvider.GetRequiredService<IEmailSyncService>();

                    // stoppingToken, not a request token: the run must outlive the HTTP request
                    var summary = await sync.SyncAsync(userId, stoppingToken);
                    _coordinator.MarkFinished(userId, summary, null);
                    ScheduleFollowUp(userId, summary, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _coordinator.MarkFinished(userId, null, "Sync was interrupted by an app shutdown.");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync failed for user {UserId}", userId);
                    _coordinator.MarkFinished(userId, null, "Sync failed. See the server log.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    private void ScheduleFollowUp(Guid userId, SyncSummary summary, CancellationToken ct)
    {
        var moreWaiting = summary.ReadStatus == GmailReadStatus.Success
            && !summary.CheckpointAdvanced
            && summary.Failed == 0
            && summary.Read > 0;

        if (!moreWaiting) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(SyncCoordinator.CooldownSeconds + 2), ct);
                _coordinator.TryRequest(userId);   // ignored if the user or a poll started one meanwhile
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }, ct);
    }
}