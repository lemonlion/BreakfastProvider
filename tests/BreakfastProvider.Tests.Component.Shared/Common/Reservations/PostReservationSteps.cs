using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Reservations;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Reservations;

public class PostReservationSteps(RequestContext context)
{
    public TestReservationRequest Request { get; set; } = new();
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestReservationResponse? Response { get; private set; }

    public async Task Send()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Reservations)
        {
            Content = JsonContent.Create(Request)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        Track.That(() => responseContentIsValidJson.Should().BeTrue());
        Response = Json.Deserialize<TestReservationResponse>(content)!;
    }
}
