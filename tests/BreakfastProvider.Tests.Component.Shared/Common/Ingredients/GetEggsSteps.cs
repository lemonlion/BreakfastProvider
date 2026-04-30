using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Ingredients;

namespace BreakfastProvider.Tests.Component.Shared.Common.Ingredients;

public class GetEggsSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestEggsResponse EggsResponse { get; private set; } = new();

    public async Task Retrieve()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Eggs);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
        EggsResponse = (await ResponseMessage.Content.ReadFromJsonAsync<TestEggsResponse>())!;
    }
}
