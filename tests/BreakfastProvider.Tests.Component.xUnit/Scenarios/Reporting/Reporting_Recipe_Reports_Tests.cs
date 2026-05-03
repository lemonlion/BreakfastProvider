using System.Net;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Reporting;

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
    public async Task Recipe_reports_should_contain_ingested_recipe_data()
    {
        // Given a pancake batch has been created
        await _milkSteps.Retrieve();
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _eggsSteps.Retrieve();
        Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _flourSteps.Retrieve();
        Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response.Should().NotBeNull());
        Track.That(() => _pancakeSteps.Response!.BatchId.Should().NotBeEmpty());

        // When the recipe reports are queried via GraphQL
        await _graphQlSteps.QueryRecipeReports(waitForOrderId: _pancakeSteps.Response?.BatchId);

        // Then the response should contain the ingested recipe reports
        Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _graphQlSteps.ParseRecipeReportsResponse();
        var batchId = _pancakeSteps.Response!.BatchId;
        Track.That(() => _graphQlSteps.RecipeReports.Should().Contain(r =>
            r.OrderId == batchId &&
            r.RecipeType == "Pancakes" &&
            r.Ingredients.Contains("Milk")));
    }

    [Fact]
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
        Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _graphQlSteps.ParseIngredientUsageResponse();
        Track.That(() => _graphQlSteps.IngredientUsage.Should().Contain(i =>
            i.Ingredient == "Milk" && i.Count >= 2));
        Track.That(() => _graphQlSteps.IngredientUsage.Should().Contain(i =>
            i.Ingredient == "Butter" && i.Count >= 1));
    }

    [Fact]
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
        Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _graphQlSteps.ParsePopularRecipesResponse();
        var pancakes = _graphQlSteps.PopularRecipes!.FirstOrDefault(r => r.RecipeType == "Pancakes");
        Track.That(() => pancakes.Should().NotBeNull());
        Track.That(() => pancakes!.Count.Should().BeGreaterThanOrEqualTo(2));
        Track.That(() => _graphQlSteps.PopularRecipes.Should().Contain(r =>
            r.RecipeType == "Waffles" && r.Count >= 1));
    }
}
