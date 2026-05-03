using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Feedback;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Feedback;

public class GetFeedbackSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestFeedbackResponse? Response { get; private set; }
    public List<TestFeedbackResponse>? ListResponse { get; private set; }

    public async Task RetrieveById(string feedbackId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.Feedback}/{feedbackId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task RetrieveByOrderId(string orderId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.Feedback}/order/{orderId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => Json.IsValid(content).Should().BeTrue());
        Response = Json.Deserialize<TestFeedbackResponse>(content)!;
    }

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => Json.IsValid(content).Should().BeTrue());
        ListResponse = Json.Deserialize<List<TestFeedbackResponse>>(content)!;
    }
}
