namespace BreakfastProvider.Api.Configuration;

public class CosmosConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "BreakfastDb";
    public int RequestTimeoutSeconds { get; set; } = 15;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool UseGatewayMode { get; set; }
}
