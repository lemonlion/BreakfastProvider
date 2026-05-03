using System.Net;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using Microsoft.Extensions.DependencyInjection;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Reporting__Recipe_Reports_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly GraphQlReportingSteps _graphQlSteps;
    private readonly Guid _recipeOrderId1 = Guid.NewGuid();
    private readonly Guid _recipeOrderId2 = Guid.NewGuid();
    private readonly Guid _recipeOrderId3 = Guid.NewGuid();

    public Reporting__Recipe_Reports_Feature()
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

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsDirectDatabaseAccess)]
    private async Task<CompositeStep> Multiple_recipe_logs_have_been_ingested_with_overlapping_ingredients()
    {
        return Sub.Steps(
            _ => A_pancake_recipe_is_seeded_with_common_ingredients(),
            _ => A_waffle_recipe_is_seeded_with_overlapping_ingredients());
    }

    private async Task A_pancake_recipe_is_seeded_with_common_ingredients()
    {
        using var scope = AppFactory.Services.CreateScope();
        var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

        await ingester.IngestRecipeLogAsync(
            _recipeOrderId1,
            "Pancakes",
            ["Milk", "Eggs", "Flour"],
            ["Maple Syrup"],
            DateTime.UtcNow);
    }

    private async Task A_waffle_recipe_is_seeded_with_overlapping_ingredients()
    {
        using var scope = AppFactory.Services.CreateScope();
        var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

        await ingester.IngestRecipeLogAsync(
            _recipeOrderId2,
            "Waffles",
            ["Milk", "Eggs", "Flour", "Butter"],
            ["Whipped Cream"],
            DateTime.UtcNow);
    }

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsDirectDatabaseAccess)]
    private async Task<CompositeStep> Multiple_recipe_logs_of_different_types_have_been_ingested()
    {
        return Sub.Steps(
            _ => Two_pancake_recipes_are_seeded(),
            _ => One_waffle_recipe_is_seeded());
    }

    private async Task Two_pancake_recipes_are_seeded()
    {
        using var scope = AppFactory.Services.CreateScope();
        var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

        await ingester.IngestRecipeLogAsync(
            _recipeOrderId1,
            "Pancakes",
            ["Milk", "Eggs", "Flour"],
            ["Maple Syrup"],
            DateTime.UtcNow);

        await ingester.IngestRecipeLogAsync(
            _recipeOrderId2,
            "Pancakes",
            ["Milk", "Eggs"],
            ["Blueberries"],
            DateTime.UtcNow);
    }

    private async Task One_waffle_recipe_is_seeded()
    {
        using var scope = AppFactory.Services.CreateScope();
        var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

        await ingester.IngestRecipeLogAsync(
            _recipeOrderId3,
            "Waffles",
            ["Milk", "Eggs", "Flour", "Butter"],
            ["Whipped Cream"],
            DateTime.UtcNow);
    }

    #endregion

    #region When

    private async Task The_recipe_reports_are_queried_via_graphql()
        => await _graphQlSteps.QueryRecipeReports(waitForOrderId: _pancakeSteps.Response?.BatchId);

    private async Task The_ingredient_usage_is_queried_via_graphql()
        => await _graphQlSteps.QueryIngredientUsage();

    private async Task The_popular_recipes_are_queried_via_graphql()
        => await _graphQlSteps.QueryPopularRecipes();

    #endregion

    #region Then

    private async Task<CompositeStep> The_graphql_response_should_contain_the_ingested_recipe_reports()
    {
        return Sub.Steps(
            _ => The_recipe_reports_response_should_be_successful(),
            _ => The_recipe_reports_response_should_be_valid_json(),
            _ => The_recipe_reports_should_contain_the_pancake_entry());
    }

    private async Task The_recipe_reports_response_should_be_successful()
        => Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_recipe_reports_response_should_be_valid_json()
        => await _graphQlSteps.ParseRecipeReportsResponse();

    private async Task The_recipe_reports_should_contain_the_pancake_entry()
    {
        var batchId = _pancakeSteps.Response!.BatchId;
        Track.That(() => _graphQlSteps.RecipeReports.Should().Contain(r =>
            r.OrderId == batchId &&
            r.RecipeType == "Pancakes" &&
            r.Ingredients.Contains("Milk")));
    }

    private async Task<CompositeStep> The_ingredient_usage_should_reflect_aggregated_counts()
    {
        return Sub.Steps(
            _ => The_ingredient_usage_response_should_be_successful(),
            _ => The_ingredient_usage_response_should_be_valid_json(),
            _ => Milk_should_appear_in_two_recipes(),
            _ => Butter_should_appear_in_one_recipe());
    }

    private async Task The_ingredient_usage_response_should_be_successful()
        => Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_ingredient_usage_response_should_be_valid_json()
        => await _graphQlSteps.ParseIngredientUsageResponse();

    private async Task Milk_should_appear_in_two_recipes()
    {
        Track.That(() => _graphQlSteps.IngredientUsage.Should().Contain(i =>
            i.Ingredient == "Milk" && i.Count >= 2));
    }

    private async Task Butter_should_appear_in_one_recipe()
    {
        Track.That(() => _graphQlSteps.IngredientUsage.Should().Contain(i =>
            i.Ingredient == "Butter" && i.Count >= 1));
    }

    private async Task<CompositeStep> The_popular_recipes_should_be_ordered_by_count_descending()
    {
        return Sub.Steps(
            _ => The_popular_recipes_response_should_be_successful(),
            _ => The_popular_recipes_response_should_be_valid_json(),
            _ => Pancakes_should_be_the_most_popular_recipe(),
            _ => Waffles_should_also_be_in_the_results());
    }

    private async Task The_popular_recipes_response_should_be_successful()
        => Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_popular_recipes_response_should_be_valid_json()
        => await _graphQlSteps.ParsePopularRecipesResponse();

    private async Task Pancakes_should_be_the_most_popular_recipe()
    {
        var pancakes = _graphQlSteps.PopularRecipes!.FirstOrDefault(r => r.RecipeType == "Pancakes");
        Track.That(() => pancakes.Should().NotBeNull());
        Track.That(() => pancakes!.Count.Should().BeGreaterThanOrEqualTo(2));
    }

    private async Task Waffles_should_also_be_in_the_results()
    {
        Track.That(() => _graphQlSteps.PopularRecipes.Should().Contain(r =>
            r.RecipeType == "Waffles" && r.Count >= 1));
    }

    #endregion
}
