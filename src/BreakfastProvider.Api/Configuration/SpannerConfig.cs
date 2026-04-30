namespace BreakfastProvider.Api.Configuration;

public class SpannerConfig
{
    public string ProjectId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string ConnectionString => $"Data Source=projects/{ProjectId}/instances/{InstanceId}/databases/{DatabaseId}";
}
