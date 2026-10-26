using System.Collections.Concurrent;
using System.Threading.Channels;
using backend.Interfaces;

namespace backend.Services;

public enum SyncRunState { Idle, Queued, Running }

public record SyncSnapshot(
    SyncRunState State,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    SyncSummary? LastSummary,
    string? LastError,
    int CooldownRemainingSeconds);

// Holds the per-user lock, the queue of requested syncs, and the result of the last run.
// State lives in memory only: it resets when the app restarts.
public class SyncCoordinator
{
    private const int CooldownSeconds = 60;   // minimum gap between the end of one run and the next request

    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();
    private readonly ConcurrentDictionary<Guid, UserSyncState> _states = new();

    public ChannelReader<Guid> Reader => _queue.Reader;

    public (SyncRequestResult Result, int RetryAfterSeconds) TryRequest(Guid userId)
    {
        var state = _states.GetOrAdd(userId, _ => new UserSyncState());

        lock (state)
        {
            if (state.State != SyncRunState.Idle)
                return (SyncRequestResult.AlreadyRunning, 0);

            var wait = RemainingCooldown(state);
            if (wait > 0)
                return (SyncRequestResult.CooldownActive, wait);

            state.State = SyncRunState.Queued;
            _queue.Writer.TryWrite(userId);
            return (SyncRequestResult.Started, 0);
        }
    }

    public void MarkStarted(Guid userId)
    {
        var state = _states.GetOrAdd(userId, _ => new UserSyncState());
        lock (state)
        {
            state.State = SyncRunState.Running;
            state.StartedAt = DateTime.Now;
        }
    }

    public void MarkFinished(Guid userId, SyncSummary? summary, string? error)
    {
        var state = _states.GetOrAdd(userId, _ => new UserSyncState());
        lock (state)
        {
            state.State = SyncRunState.Idle;
            state.FinishedAt = DateTime.Now;
            state.LastSummary = summary;
            state.LastError = error;
        }
    }

    public SyncSnapshot GetSnapshot(Guid userId)
    {
        if (!_states.TryGetValue(userId, out var state))
            return new SyncSnapshot(SyncRunState.Idle, null, null, null, null, 0);

        lock (state)
        {
            return new SyncSnapshot(state.State, state.StartedAt, state.FinishedAt,
                state.LastSummary, state.LastError, RemainingCooldown(state));
        }
    }

    // Caller must hold the lock on state
    private static int RemainingCooldown(UserSyncState state)
    {
        if (state.State != SyncRunState.Idle || state.FinishedAt is null) return 0;
        var remaining = CooldownSeconds - (int)(DateTime.Now - state.FinishedAt.Value).TotalSeconds;
        return remaining > 0 ? remaining : 0;
    }

    private class UserSyncState
    {
        public SyncRunState State { get; set; } = SyncRunState.Idle;
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public SyncSummary? LastSummary { get; set; }
        public string? LastError { get; set; }
    }
}