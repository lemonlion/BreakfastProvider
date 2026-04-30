using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BreakfastProvider.Api.Events;

/// <summary>
/// Message headers, in both legacy and CloudEvents formats.
/// </summary>
[Description("Message headers, in both legacy and CloudEvents formats.")]
public class MessageHeaders
{
    //
    // CloudEvents headers.  Should all be serialised in the format of `ce_headername` except for `content-type`
    //

    [JsonPropertyName("ce_specversion")]
    [Description("Version of the CloudEvents specification.")]
    public string CeSpecVersion { get; init; } = "";

    [JsonPropertyName("ce_type")]
    [Description("Type of event.")]
    public string CeType { get; init; } = "";

    [JsonPropertyName("ce_source")]
    [Description("Issuer URL.")]
    public string CeSource { get; init; } = "";

    [JsonPropertyName("ce_id")]
    [Description("ID of the event.")]
    public Guid CeId { get; init; }

    [JsonPropertyName("ce_time")]
    [Description("Timestamp of the event (ISO 8601 format).")]
    public DateTime CeTime { get; init; }

    [JsonPropertyName("ce_traceparent")]
    [Description("Correlation ID.")]
    public string CeTraceParent { get; init; } = "";

    [JsonPropertyName("ce_tenant")]
    [Description("Tenant ID.")]
    public string CeTenant { get; init; } = "";

    [JsonPropertyName("content-type")]
    [Description("Content type.")]
    public string ContentType { get; init; } = "";
}
