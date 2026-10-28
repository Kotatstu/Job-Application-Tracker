using backend.Interfaces;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class PollingWorker : BackgroundService
{
    private readonly SyncCoordinator _coordinator;
    private readonly IServiceScopeFactory _scopes;
    private readonly SyncOptions _options;
    private readonly ILogger<PollingWorker> _logger;

    public PollingWorker(SyncCoordinator coordinator, IServiceScopeFactory scopes,
        IOptions<SyncOptions> options, ILogger<PollingWorker> logger)
    {
        _coordinator = coordinator;
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.PollingEnabled)
        {
            _logger.LogInformation("Mailbox polling is disabled");
            return;
        }

        _logger.LogInformation("Mailbox polling every {Minutes} min, first check in {Seconds} s",
            _options.PollIntervalMinutes, _options.StartupDelaySeconds);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.PollIntervalMinutes));
            do
            {
                await PollOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var mailbox = scope.ServiceProvider.GetRequiredService<IMailboxService>();

            foreach (var userId in await mailbox.GetConnectedUserIdsAsync())
            {
                var status = await mailbox.GetStatusAsync(userId);
                if (status.NeedsReconnect)
                {
                    _logger.LogDebug("Polling skipped: reconnect needed");
                    continue;
                }

                var (result, _) = await mailbox.RequestSyncAsync(userId);

                if (result == SyncRequestResult.Started)
                    _logger.LogInformation("Polling started a sync");
                else
                    _logger.LogDebug("Polling did not start a sync: {Result}", result);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Polling check failed");
        }
    }
}