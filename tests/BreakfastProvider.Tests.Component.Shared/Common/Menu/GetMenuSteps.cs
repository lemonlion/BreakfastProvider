using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Menu;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Menu;

public class GetMenuSteps(RequestContext context)
{
    private readonly Dictionary<string, string> _extraHeaders = new();

    public HttpResponseMessage? ResponseMessage { get; private set; }
    public List<TestMenuItemResponse>? Response { get; private set; }

    public void AddHeader(string name, string value) => _extraHeaders[name] = value;

    public async Task Retrieve()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        foreach (var header in _extraHeaders)
            request.Headers.Add(header.Key, header.Value);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => Json.IsValid(content).Should().BeTrue());
        Response = Json.Deserialize<List<TestMenuItemResponse>>(content)!;
    }
}
