using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Reservations;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Reservations;

public class CancelReservationSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestReservationResponse? Response { get; private set; }

    public async Task Send(int id)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{Endpoints.Reservations}/{id}/cancel");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Json.IsValid(content).Should().BeTrue();
        Response = Json.Deserialize<TestReservationResponse>(content)!;
    }
}
