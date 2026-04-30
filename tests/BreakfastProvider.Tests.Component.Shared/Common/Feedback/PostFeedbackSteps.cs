using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Feedback;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Feedback;

public class PostFeedbackSteps(RequestContext context)
{
    public TestFeedbackRequest Request { get; set; } = new();
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestFeedbackResponse? Response { get; private set; }

    public async Task Send()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Feedback)
        {
            Content = JsonContent.Create(Request)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Json.IsValid(content).Should().BeTrue();
        Response = Json.Deserialize<TestFeedbackResponse>(content)!;
    }
}
