namespace BreakfastProvider.Api.Configuration;

public class ClickHouseConfig
{
    /// <summary>
    /// ClickHouse.Client connection string, e.g. <c>Host=localhost;Port=8123;Database=kitchen_analytics</c>.
    /// Empty means ClickHouse is disabled.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
