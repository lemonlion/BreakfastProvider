using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Reporting;

[Binding]
public class BatchCompletionsSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps,
    GraphQlReportingSteps graphQlSteps)
{
    [Given("a pancake batch has been created for batch completions")]
    public async Task GivenAPancakeBatchHasBeenCreatedForBatchCompletions()
    {
        await milkSteps.Retrieve();
        milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await eggsSteps.Retrieve();
        eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await flourSteps.Retrieve();
        flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

        pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = milkSteps.MilkResponse.Milk,
            Eggs = eggsSteps.EggsResponse.Eggs,
            Flour = flourSteps.FlourResponse.Flour
        };
        await pancakeSteps.Send();
        pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await pancakeSteps.ParseResponse();
        pancakeSteps.Response.Should().NotBeNull();
        pancakeSteps.Response!.BatchId.Should().NotBeEmpty();
    }

    [When("the batch completions are queried via graphql")]
    public async Task WhenTheBatchCompletionsAreQueriedViaGraphql()
    {
        await graphQlSteps.QueryBatchCompletions(waitForBatchId: pancakeSteps.Response?.BatchId);
    }

    [Then("the graphql response should contain the batch completion record")]
    public async Task ThenTheGraphqlResponseShouldContainTheBatchCompletionRecord()
    {
        graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await graphQlSteps.ParseBatchCompletionsResponse();
        var batchId = pancakeSteps.Response!.BatchId;
        graphQlSteps.BatchCompletions.Should().Contain(r =>
            r.BatchId == batchId &&
            r.RecipeType == "Pancakes" &&
            r.Ingredients.Contains("Milk"));
    }
}
