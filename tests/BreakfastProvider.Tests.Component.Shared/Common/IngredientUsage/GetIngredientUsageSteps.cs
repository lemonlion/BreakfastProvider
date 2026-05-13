using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientUsage;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.IngredientUsage;

public class GetIngredientUsageSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public List<TestIngredientUsageResponse>? ListResponse { get; private set; }
    public List<TestIngredientUsageSummaryResponse>? SummaryResponse { get; private set; }

    public async Task RetrieveSummary()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.IngredientUsage}/summary");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task RetrieveByIngredient(string ingredientName)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.IngredientUsage}/ingredient/{ingredientName}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        responseContentIsValidJson.Should().BeTrue();
        ListResponse = Json.Deserialize<List<TestIngredientUsageResponse>>(content)!;
    }

    public async Task ParseSummaryResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        responseContentIsValidJson.Should().BeTrue();
        SummaryResponse = Json.Deserialize<List<TestIngredientUsageSummaryResponse>>(content)!;
    }
}
