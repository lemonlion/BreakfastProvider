using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Reservations;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Reservations;

public class GetReservationSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestReservationResponse? Response { get; private set; }
    public List<TestReservationResponse>? ListResponse { get; private set; }

    public async Task RetrieveById(int id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.Reservations}/{id}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task RetrieveAll()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Reservations);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => Json.IsValid(content).Should().BeTrue());
        Response = Json.Deserialize<TestReservationResponse>(content)!;
    }

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => Json.IsValid(content).Should().BeTrue());
        ListResponse = Json.Deserialize<List<TestReservationResponse>>(content)!;
    }
}
