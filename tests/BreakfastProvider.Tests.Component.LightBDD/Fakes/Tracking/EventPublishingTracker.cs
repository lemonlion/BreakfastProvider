using System.Text.Json;
using BreakfastProvider.Api;
using Microsoft.AspNetCore.Http;
using BreakfastProvider.Tests.Component.Shared.Constants;
using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.LightBDD.Fakes.Tracking;

/// <summary>
/// Reusable helper that logs event-publishing request/response pairs to the
/// <see cref="RequestResponseLogger"/> used by TestTrackingDiagrams.
/// This makes published events (Kafka, EventGrid, etc.) visible in the
/// PlantUML sequence diagrams generated in the HTML specification reports.
/// </summary>
public class EventPublishingTracker(
    IHttpContextAccessor httpContextAccessor,
    JsonSerializerOptions? serializerOptions = null)
{
    private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? new JsonSerializerOptions();

    private record CurrentTestInfo(string TestName, Guid TestId, string CallerName, Guid TraceId);

    private CurrentTestInfo GetTestInfo()
    {
        httpContextAccessor.HttpContext!.Request.Headers.TryGetValue(TestTrackingHttpHeaders.TraceIdHeader, out var traceIdHeaders);
        httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestNameHeader, out var currentTestNameHeaders);
        httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestIdHeader, out var currentTestIdHeaders);
        httpContextAccessor.HttpContext.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CallerNameHeader, out var callerNameHeaders);

        return new CurrentTestInfo(
            currentTestNameHeaders.First()!,
            Guid.Parse(currentTestIdHeaders.First()!),
            callerNameHeaders.First()!,
            Guid.Parse(traceIdHeaders.First()!));
    }

    /// <summary>
    /// Logs a request entry for an event being published to a named destination.
    /// Returns a correlation ID to pass to <see cref="CreateResponseLog"/>.
    /// </summary>
    public Guid CreateRequestLog(string protocol, string destinationServiceName, Uri destinationUri, object eventPayload)
    {
        var requestResponseId = Guid.NewGuid();
        var requestContentString = JsonSerializer.Serialize(eventPayload, _serializerOptions);
        var currentTestInfo = GetTestInfo();

        RequestResponseLogger.Log(new RequestResponseLog(
            currentTestInfo.TestName,
            currentTestInfo.TestId.ToString(),
            protocol,
            requestContentString,
            destinationUri,
            [],
            destinationServiceName,
            Documentation.ServiceNames.BreakfastProvider,
            RequestResponseType.Request,
            currentTestInfo.TraceId,
            requestResponseId,
            false,
            MetaType: RequestResponseMetaType.Event
        ));

        return requestResponseId;
    }

    /// <summary>
    /// Logs a response entry for an event that was published.
    /// </summary>
    public void CreateResponseLog(string protocol, string destinationServiceName, Uri destinationUri, Guid requestResponseId, object? responsePayload = null)
    {
        var responseContentString = responsePayload is not null
            ? JsonSerializer.Serialize(responsePayload, _serializerOptions)
            : string.Empty;
        var currentTestInfo = GetTestInfo();

        RequestResponseLogger.Log(new RequestResponseLog(
            currentTestInfo.TestName,
            currentTestInfo.TestId.ToString(),
            protocol,
            responseContentString,
            destinationUri,
            [],
            destinationServiceName,
            Documentation.ServiceNames.BreakfastProvider,
            RequestResponseType.Response,
            currentTestInfo.TraceId,
            requestResponseId,
            false,
            "Responded",
            MetaType: RequestResponseMetaType.Event
        ));
    }
}
