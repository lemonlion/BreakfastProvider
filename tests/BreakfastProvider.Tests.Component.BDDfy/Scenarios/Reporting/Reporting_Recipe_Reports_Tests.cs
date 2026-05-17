using System.Net;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Microsoft.Extensions.DependencyInjection;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Reporting;

public class Reporting_Recipe_Reports_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly GraphQlReportingSteps _graphQlSteps;
    private readonly Guid _recipeOrderId1 = Guid.NewGuid();
    private readonly Guid _recipeOrderId2 = Guid.NewGuid();
    private readonly Guid _recipeOrderId3 = Guid.NewGuid();

    public Reporting_Recipe_Reports_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _graphQlSteps = Get<GraphQlReportingSteps>();
    }

    [Fact]
    [HappyPath]
    public void Recipe_reports_should_contain_ingested_recipe_data()
    {
        this.Given(x => x.A_pancake_batch_has_been_created())
            .When(x => x.The_recipe_reports_are_queried_via_graphql())
            .Then(x => x.The_response_should_contain_the_ingested_recipe_reports())
            .BDDfy();
    }

    [Fact]
    public void Ingredient_usage_should_aggregate_across_multiple_recipes()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.Multiple_recipe_logs_have_been_ingested_with_overlapping_ingredients())
            .When(x => x.The_ingredient_usage_is_queried_via_graphql())
            .Then(x => x.The_ingredient_usage_should_reflect_aggregated_counts())
            .BDDfy();
    }

    [Fact]
    public void Popular_recipes_should_return_recipe_types_ordered_by_frequency()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.Multiple_recipe_logs_of_different_types_have_been_ingested())
            .When(x => x.The_popular_recipes_are_queried_via_graphql())
            .Then(x => x.The_popular_recipes_should_be_ordered_by_count_descending())
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

    private async Task The_recipe_reports_are_queried_via_graphql()
    {
        await _graphQlSteps.QueryRecipeReports(waitForOrderId: _pancakeSteps.Response?.BatchId);
    }

    private async Task The_response_should_contain_the_ingested_recipe_reports()
    {
        _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _graphQlSteps.ParseRecipeReportsResponse();
        var batchId = _pancakeSteps.Response!.BatchId;
        _graphQlSteps.RecipeReports.Should().Contain(r =>
            r.OrderId == batchId &&
            r.RecipeType == "Pancakes" &&
            r.Ingredients.Contains("Milk"));
    }

    private async Task Multiple_recipe_logs_have_been_ingested_with_overlapping_ingredients()
    {
        using (var scope = AppFactory.Services.CreateScope())
        {
            var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();
            await ingester.IngestRecipeLogAsync(
                _recipeOrderId1, "Pancakes", ["Milk", "Eggs", "Flour"], ["Maple Syrup"], DateTime.UtcNow);
        }

        using (var scope = AppFactory.Services.CreateScope())
        {
            var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();
            await ingester.IngestRecipeLogAsync(
                _recipeOrderId2, "Waffles", ["Milk", "Eggs", "Flour", "Butter"], ["Whipped Cream"], DateTime.UtcNow);
        }
    }

    private async Task The_ingredient_usage_is_queried_via_graphql()
    {
        await _graphQlSteps.QueryIngredientUsage();
    }

    private async Task The_ingredient_usage_should_reflect_aggregated_counts()
    {
        _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _graphQlSteps.ParseIngredientUsageResponse();
        _graphQlSteps.IngredientUsage.Should().Contain(i =>
            i.Ingredient == "Milk" && i.Count >= 2);
        _graphQlSteps.IngredientUsage.Should().Contain(i =>
            i.Ingredient == "Butter" && i.Count >= 1);
    }

    private async Task Multiple_recipe_logs_of_different_types_have_been_ingested()
    {
        using (var scope = AppFactory.Services.CreateScope())
        {
            var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();
            await ingester.IngestRecipeLogAsync(
                _recipeOrderId1, "Pancakes", ["Milk", "Eggs", "Flour"], ["Maple Syrup"], DateTime.UtcNow);
            await ingester.IngestRecipeLogAsync(
                _recipeOrderId2, "Pancakes", ["Milk", "Eggs"], ["Blueberries"], DateTime.UtcNow);
        }

        using (var scope = AppFactory.Services.CreateScope())
        {
            var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();
            await ingester.IngestRecipeLogAsync(
                _recipeOrderId3, "Waffles", ["Milk", "Eggs", "Flour", "Butter"], ["Whipped Cream"], DateTime.UtcNow);
        }
    }

    private async Task The_popular_recipes_are_queried_via_graphql()
    {
        await _graphQlSteps.QueryPopularRecipes();
    }

    private async Task The_popular_recipes_should_be_ordered_by_count_descending()
    {
        _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _graphQlSteps.ParsePopularRecipesResponse();
        var pancakes = _graphQlSteps.PopularRecipes!.FirstOrDefault(r => r.RecipeType == "Pancakes");
        pancakes.Should().NotBeNull();
        pancakes!.Count.Should().BeGreaterThanOrEqualTo(2);
        _graphQlSteps.PopularRecipes.Should().Contain(r =>
            r.RecipeType == "Waffles" && r.Count >= 1);
    }

    #endregion
}
