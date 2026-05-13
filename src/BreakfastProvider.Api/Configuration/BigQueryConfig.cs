namespace BreakfastProvider.Api.Configuration;

public class BigQueryConfig
{
    public string ProjectId { get; set; } = string.Empty;
    public string DatasetId { get; set; } = "breakfast_analytics";
    public string EmulatorEndpoint { get; set; } = string.Empty;
}
