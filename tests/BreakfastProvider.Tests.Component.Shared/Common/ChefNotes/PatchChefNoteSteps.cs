using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Common;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.ChefNotes;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.ChefNotes;

public class PatchChefNoteSteps(RequestContext context)
{
    public TestUpdateChefNoteRequest Request { get; set; } = new();
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestChefNoteResponse? Response { get; private set; }

    public async Task Send(string noteId)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{Endpoints.ChefNotes}/{noteId}")
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
        responseContentIsValidJson.Should().BeTrue();
        Response = Json.Deserialize<TestChefNoteResponse>(content)!;
    }
}
