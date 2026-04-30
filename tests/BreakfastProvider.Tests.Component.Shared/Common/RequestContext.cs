namespace BreakfastProvider.Tests.Component.Shared.Common;

public class RequestContext(Func<HttpClient> clientFactory, string requestId)
{
    public HttpClient Client => clientFactory();
    public string RequestId { get; } = requestId;
}
