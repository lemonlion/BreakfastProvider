using BreakfastProvider.Tests.Component.Shared.Common;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.ChefNotes;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.ChefNotes;

public class GetChefNoteSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestChefNoteResponse? Response { get; private set; }
    public List<TestChefNoteResponse>? ListResponse { get; private set; }

    public async Task RetrieveById(string noteId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.ChefNotes}/{noteId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task RetrieveByRecipe(string recipeName)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.ChefNotes}/recipe/{recipeName}");
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

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        responseContentIsValidJson.Should().BeTrue();
        ListResponse = Json.Deserialize<List<TestChefNoteResponse>>(content)!;
    }
}
