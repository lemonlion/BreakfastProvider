using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Toppings;

public class GetToppingsSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public List<TestToppingResponse>? Response { get; private set; }

    public async Task Retrieve()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Toppings);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        Track.That(() => responseContentIsValidJson.Should().BeTrue());
        Response = Json.Deserialize<List<TestToppingResponse>>(content)!;
    }
}
