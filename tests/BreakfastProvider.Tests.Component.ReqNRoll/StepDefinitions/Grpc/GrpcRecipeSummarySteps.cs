using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using Grpc.Core;
using Reqnroll;
using TestTrackingDiagrams.ReqNRoll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Grpc;

[Binding]
public class GrpcRecipeSummarySteps(AppManager appManager)
{
    private readonly GrpcBreakfastSteps _grpcSteps = new();

    private void EnsureGrpcClient()
    {
        if (!AppManager.Settings.RunAgainstExternalServiceUnderTest)
            _grpcSteps.Initialize(appManager.AppFactory, CurrentTestInfo.Fetcher);
    }

    [When(@"a recipe summary is requested for ""(.*)"" via gRPC")]
    public async Task WhenARecipeSummaryIsRequestedViaGrpc(string recipeType)
    {
        EnsureGrpcClient();
        await _grpcSteps.GetRecipeSummary(recipeType);
    }

    [Then(@"the recipe summary should contain (\d+) total batches")]
    public void ThenTheRecipeSummaryShouldContainTotalBatches(int expectedBatches)
    {
        Track.That(() => _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().Be(expectedBatches));
    }

    [Then(@"the recipe summary should contain ingredients ""(.*)""")]
    public void ThenTheRecipeSummaryShouldContainIngredients(string ingredientsCsv)
    {
        var expected = ingredientsCsv.Split(',', StringSplitOptions.TrimEntries);
        Track.That(() => _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEquivalentTo(expected));
    }

    [Then("the recipe summary should contain no ingredients")]
    public void ThenTheRecipeSummaryShouldContainNoIngredients()
    {
        Track.That(() => _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEmpty());
    }
}
