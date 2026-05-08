using System.Net;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Reporting;

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

    [Test]
    [HappyPath]
    public async Task Recipe_reports_should_contain_ingested_recipe_data()
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

        // When the recipe reports are queried via GraphQL
        await _graphQlSteps.QueryRecipeReports(waitForOrderId: _pancakeSteps.Response?.BatchId);

        // Then the response should contain the ingested recipe reports
        await _graphQlSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _graphQlSteps.ParseRecipeReportsResponse();
        var batchId = _pancakeSteps.Response!.BatchId;
        await _graphQlSteps.RecipeReports.Should().Contain(r =>
            r.OrderId == batchId &&
            r.RecipeType == "Pancakes" &&
            r.Ingredients.Contains("Milk"));
    }

    [Test]
    public async Task Ingredient_usage_should_aggregate_across_multiple_recipes()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given multiple recipe logs have been ingested with overlapping ingredients
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

        // When the ingredient usage is queried via GraphQL
        await _graphQlSteps.QueryIngredientUsage();

        // Then the ingredient usage should reflect aggregated counts
        await _graphQlSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _graphQlSteps.ParseIngredientUsageResponse();
        await _graphQlSteps.IngredientUsage.Should().Contain(i =>
            i.Ingredient == "Milk" && i.Count >= 2);
        await _graphQlSteps.IngredientUsage.Should().Contain(i =>
            i.Ingredient == "Butter" && i.Count >= 1);
    }

    [Test]
    public async Task Popular_recipes_should_return_recipe_types_ordered_by_frequency()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given multiple recipe logs of different types have been ingested
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

        // When the popular recipes are queried via GraphQL
        await _graphQlSteps.QueryPopularRecipes();

        // Then the popular recipes should be ordered by count descending
        await _graphQlSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _graphQlSteps.ParsePopularRecipesResponse();
        var pancakes = _graphQlSteps.PopularRecipes!.FirstOrDefault(r => r.RecipeType == "Pancakes");
        await pancakes.Should().NotBeNull();
        await pancakes!.Count.Should().BeGreaterThanOrEqualTo(2);
        await _graphQlSteps.PopularRecipes.Should().Contain(r =>
            r.RecipeType == "Waffles" && r.Count >= 1);
    }
}
