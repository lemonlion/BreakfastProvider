using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Toppings;

public class PutToppingSteps(RequestContext context)
{
    public TestUpdateToppingRequest Request { get; set; } = new();
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestToppingResponse? Response { get; private set; }

    public async Task Send(Guid toppingId)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"{Endpoints.Toppings}/{toppingId}")
        {
            Content = JsonContent.Create(Request)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => Json.IsValid(content).Should().BeTrue());
        Response = Json.Deserialize<TestToppingResponse>(content)!;
    }
}
