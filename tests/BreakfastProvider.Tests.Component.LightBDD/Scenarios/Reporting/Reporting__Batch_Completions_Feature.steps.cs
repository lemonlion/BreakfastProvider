using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Reporting__Batch_Completions_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly GraphQlReportingSteps _graphQlSteps;

    public Reporting__Batch_Completions_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _graphQlSteps = Get<GraphQlReportingSteps>();
    }

    #region Given

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => Milk_is_retrieved_from_the_milk_endpoint(),
            _ => The_milk_response_should_be_successful(),
            _ => Eggs_are_retrieved_from_the_eggs_endpoint(),
            _ => The_eggs_response_should_be_successful(),
            _ => Flour_is_retrieved_from_the_flour_endpoint(),
            _ => The_flour_response_should_be_successful(),
            _ => A_pancake_request_is_submitted_with_all_ingredients(),
            _ => The_pancake_batch_response_should_be_successful());
    }

    private async Task Milk_is_retrieved_from_the_milk_endpoint()
        => await _milkSteps.Retrieve();

    private async Task The_milk_response_should_be_successful()
        => Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Eggs_are_retrieved_from_the_eggs_endpoint()
        => await _eggsSteps.Retrieve();

    private async Task The_eggs_response_should_be_successful()
        => Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Flour_is_retrieved_from_the_flour_endpoint()
        => await _flourSteps.Retrieve();

    private async Task The_flour_response_should_be_successful()
        => Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task A_pancake_request_is_submitted_with_all_ingredients()
    {
        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
    }

    private async Task The_pancake_batch_response_should_be_successful()
    {
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response.Should().NotBeNull());
        Track.That(() => _pancakeSteps.Response!.BatchId.Should().NotBeEmpty());
    }

    #endregion

    #region When

    private async Task The_batch_completions_are_queried_via_graphql()
        => await _graphQlSteps.QueryBatchCompletions(waitForBatchId: _pancakeSteps.Response?.BatchId);

    #endregion

    #region Then

    private async Task<CompositeStep> The_graphql_response_should_contain_the_batch_completion_record()
    {
        return Sub.Steps(
            _ => The_batch_completions_response_should_be_successful(),
            _ => The_batch_completions_response_should_be_valid_json(),
            _ => The_batch_completions_should_contain_the_pancake_batch());
    }

    private async Task The_batch_completions_response_should_be_successful()
        => Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_batch_completions_response_should_be_valid_json()
        => await _graphQlSteps.ParseBatchCompletionsResponse();

    private async Task The_batch_completions_should_contain_the_pancake_batch()
    {
        var batchId = _pancakeSteps.Response!.BatchId;
        Track.That(() => _graphQlSteps.BatchCompletions.Should().Contain(r =>
            r.BatchId == batchId &&
            r.RecipeType == "Pancakes" &&
            r.Ingredients.Contains("Milk")));
    }

    #endregion
}
