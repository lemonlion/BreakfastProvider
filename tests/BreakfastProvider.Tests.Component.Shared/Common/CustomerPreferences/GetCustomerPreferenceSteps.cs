using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerPreferences;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.CustomerPreferences;

public class GetCustomerPreferenceSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestCustomerPreferenceResponse? Response { get; private set; }

    public async Task RetrieveById(string customerId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.CustomerPreferences}/{customerId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        Track.That(() => responseContentIsValidJson.Should().BeTrue());
        Response = Json.Deserialize<TestCustomerPreferenceResponse>(content)!;
    }
}
