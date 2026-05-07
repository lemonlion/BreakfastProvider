using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Reporting;

public class Reporting_Batch_Completions_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly GraphQlReportingSteps _graphQlSteps;

    public Reporting_Batch_Completions_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _graphQlSteps = Get<GraphQlReportingSteps>();
    }

    [Fact]
    [HappyPath]
    public void Batch_completions_should_contain_data_ingested_via_pubsub_consumer()
    {
        this.Given(x => x.A_pancake_batch_has_been_created())
            .When(x => x.The_batch_completions_are_queried_via_graphql())
            .Then(x => x.The_response_should_contain_the_batch_completion_record())
            .BDDfy();
    }

    #region Steps

    private async Task A_pancake_batch_has_been_created()
    {
        await _milkSteps.Retrieve();
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _eggsSteps.Retrieve();
        _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _flourSteps.Retrieve();
        _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        _pancakeSteps.Response.Should().NotBeNull();
        _pancakeSteps.Response!.BatchId.Should().NotBeEmpty();
    }

    private async Task The_batch_completions_are_queried_via_graphql()
    {
        await _graphQlSteps.QueryBatchCompletions(waitForBatchId: _pancakeSteps.Response?.BatchId);
    }

    private async Task The_response_should_contain_the_batch_completion_record()
    {
        _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _graphQlSteps.ParseBatchCompletionsResponse();
        var batchId = _pancakeSteps.Response!.BatchId;
        _graphQlSteps.BatchCompletions.Should().Contain(r =>
            r.BatchId == batchId &&
            r.RecipeType == "Pancakes" &&
            r.Ingredients.Contains("Milk"));
    }

    #endregion
}
