using BreakfastProvider.Tests.Component.Shared.Common;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientWaste;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.IngredientWaste;

public class GetIngredientWasteSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public List<TestIngredientWasteResponse>? ListResponse { get; private set; }

    public async Task RetrieveByRecipe(string recipeName)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.IngredientWaste}/recipe/{recipeName}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        responseContentIsValidJson.Should().BeTrue();
        ListResponse = Json.Deserialize<List<TestIngredientWasteResponse>>(content)!;
    }
}
