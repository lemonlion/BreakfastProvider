namespace BreakfastProvider.Api.Configuration;

public class EventHubConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string EventHubName { get; set; } = string.Empty;
    public string ConsumerGroup { get; set; } = "$Default";
    public string BlobStorageConnectionString { get; set; } = string.Empty;
    public string BlobContainerName { get; set; } = "eventhub-checkpoints";
}
