using System.Text.Json.Serialization;

namespace BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;

public class TestHealthCheckResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("results")]
    public Dictionary<string, TestHealthCheckEntry> Results { get; set; } = new();
}

public class TestHealthCheckEntry
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}
