namespace BreakfastProvider.Tests.Component.Shared.Common;

public class RequestContext(Func<HttpClient> clientFactory, string requestId, Func<(string Name, string Id)> currentTestInfo)
{
    public HttpClient Client => clientFactory();
    public string RequestId { get; } = requestId;

    /// <summary>
    /// The running test as Kronikol knows it, from its adapter's <c>CurrentTestInfo.Fetcher</c>. Not
    /// <see cref="RequestId"/>: that id files what the fakes receive, and a call Kronikol logs under it is in
    /// no scenario's diagram.
    /// </summary>
    public (string Name, string Id) CurrentTest => currentTestInfo();
}
