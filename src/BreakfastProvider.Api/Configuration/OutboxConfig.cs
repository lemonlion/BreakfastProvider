namespace BreakfastProvider.Api.Configuration;

public class OutboxConfig
{
    public int PollingIntervalSeconds { get; set; } = 5;
    public int MaxRetryCount { get; set; } = 3;
    public int BatchSize { get; set; } = 25;
    public bool IsEnabled { get; set; } = true;
}
