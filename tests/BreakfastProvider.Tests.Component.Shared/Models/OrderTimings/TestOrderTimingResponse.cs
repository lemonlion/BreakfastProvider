namespace BreakfastProvider.Tests.Component.Shared.Models.OrderTimings;

public class TestOrderTimingResponse
{
    public string TimingId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Station { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public decimal PrepSeconds { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class TestOrderTimingSummaryResponse
{
    public string Station { get; set; } = string.Empty;
    public decimal AvgPrepSeconds { get; set; }
    public decimal P95PrepSeconds { get; set; }
    public int TimingCount { get; set; }
}
