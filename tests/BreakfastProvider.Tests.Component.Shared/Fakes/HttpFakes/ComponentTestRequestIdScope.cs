namespace BreakfastProvider.Tests.Component.Shared.Fakes.HttpFakes;

/// <summary>
/// The component-test request id for work with no HTTP request in flight, such as an event a step publishes
/// and an in-memory consumer handles: <see cref="RequestCapturingHandler"/> files the requests that work makes
/// under it, as it files a request's under its <c>X-ComponentTest-RequestId</c> header. Kept apart from
/// Kronikol's <c>TestIdentityScope</c>, which names the scenario a call is drawn in.
/// </summary>
public static class ComponentTestRequestIdScope
{
    private static readonly AsyncLocal<string?> RequestId = new();

    public static string? Current => RequestId.Value;

    public static IDisposable Begin(string requestId)
    {
        var previous = RequestId.Value;
        RequestId.Value = requestId;
        return new Scope(previous);
    }

    private sealed class Scope(string? previous) : IDisposable
    {
        public void Dispose() => RequestId.Value = previous;
    }
}
