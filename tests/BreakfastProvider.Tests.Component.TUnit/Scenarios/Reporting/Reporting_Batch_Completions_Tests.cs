using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Reporting;

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

    [Test]
    [HappyPath]
    public async Task Batch_completions_should_contain_data_ingested_via_pubsub_consumer()
    {
        // Given a pancake batch has been created
        await _milkSteps.Retrieve();
        await _milkSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _eggsSteps.Retrieve();
        await _eggsSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _flourSteps.Retrieve();
        await _flourSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
        await _pancakeSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        await _pancakeSteps.Response.Should().NotBeNull();
        await _pancakeSteps.Response!.BatchId.Should().NotBeEqualTo(Guid.Empty);

        // When the batch completions are queried via GraphQL
        await _graphQlSteps.QueryBatchCompletions(waitForBatchId: _pancakeSteps.Response?.BatchId);

        // Then the response should contain the batch completion record
        await _graphQlSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _graphQlSteps.ParseBatchCompletionsResponse();
        var batchId = _pancakeSteps.Response!.BatchId;
        await _graphQlSteps.BatchCompletions.Should().Contain(r =>
            r.BatchId == batchId &&
            r.RecipeType == "Pancakes" &&
            r.Ingredients.Contains("Milk"));
    }
}
