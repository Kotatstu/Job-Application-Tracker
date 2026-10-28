// Services/SyncOptions.cs
using System.ComponentModel.DataAnnotations;

namespace backend.Services;

public class SyncOptions
{
    public bool PollingEnabled { get; set; } = true;

    [Range(1, 1440)]
    public int PollIntervalMinutes { get; set; } = 30;

    [Range(0, 3600)]
    public int StartupDelaySeconds { get; set; } = 60;
}