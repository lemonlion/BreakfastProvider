using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeReviews;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.RecipeReviews;

public class GetRecipeReviewSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestRecipeReviewResponse? Response { get; private set; }
    public List<TestRecipeReviewResponse>? ListResponse { get; private set; }

    public async Task RetrieveById(string reviewId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.RecipeReviews}/{reviewId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task RetrieveByRecipe(string recipeName)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.RecipeReviews}/recipe/{recipeName}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        responseContentIsValidJson.Should().BeTrue();
        Response = Json.Deserialize<TestRecipeReviewResponse>(content)!;
    }

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        responseContentIsValidJson.Should().BeTrue();
        ListResponse = Json.Deserialize<List<TestRecipeReviewResponse>>(content)!;
    }
}
