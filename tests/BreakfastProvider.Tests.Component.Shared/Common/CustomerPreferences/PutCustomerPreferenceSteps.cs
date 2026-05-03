using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerPreferences;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.CustomerPreferences;

public class PutCustomerPreferenceSteps(RequestContext context)
{
    public TestCustomerPreferenceRequest Request { get; set; } = new();
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestCustomerPreferenceResponse? Response { get; private set; }

    public async Task Send(string customerId)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"{Endpoints.CustomerPreferences}/{customerId}")
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
        Response = Json.Deserialize<TestCustomerPreferenceResponse>(content)!;
    }
}
