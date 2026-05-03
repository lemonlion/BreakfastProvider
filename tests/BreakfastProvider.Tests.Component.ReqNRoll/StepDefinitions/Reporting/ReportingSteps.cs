using System.Net;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Reporting;

[Binding]
public class ReportingSteps(
    AppManager appManager,
    GraphQlReportingSteps graphQlSteps)
{
    private readonly Guid _testOrderId = Guid.NewGuid();
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";
    private readonly Guid _recipeOrderId1 = Guid.NewGuid();
    private readonly Guid _recipeOrderId2 = Guid.NewGuid();
    private readonly Guid _recipeOrderId3 = Guid.NewGuid();

    [Given("an order has been created and ingested into the reporting database")]
    public async Task GivenAnOrderHasBeenCreatedAndIngestedIntoTheReportingDatabase()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;

        using var scope = appManager.AppFactory!.Services.CreateScope();
        var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

        await ingester.IngestOrderCreatedAsync(
            _testOrderId,
            _customerName,
            itemCount: 3,
            tableNumber: 7,
            DateTime.UtcNow);
    }

    [Given("recipe logs have been ingested into the reporting database")]
    public async Task GivenRecipeLogsHaveBeenIngestedIntoTheReportingDatabase()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;

        using var scope = appManager.AppFactory!.Services.CreateScope();
        var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

        await ingester.IngestRecipeLogAsync(
            _recipeOrderId1,
            "Pancakes",
            ["Milk", "Eggs", "Flour"],
            ["Maple Syrup"],
            DateTime.UtcNow);
    }

    [Given("multiple recipe logs have been ingested with overlapping ingredients")]
    public async Task GivenMultipleRecipeLogsHaveBeenIngestedWithOverlappingIngredients()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;

        using var scope = appManager.AppFactory!.Services.CreateScope();
        var ingester = scope.ServiceProvider.GetRequiredService<IReportingIngester>();

        await ingester.IngestRecipeLogAsync(
            _recipeOrderId1,
            "Pancakes",
            ["Milk", "Eggs", "Flour"],
            ["Maple Syrup"],
            DateTime.UtcNow);

        await ingester.IngestRecipeLogAsync(
            _recipeOrderId2,
            "Waffles",
            ["Milk", "Eggs", "Flour", "Butter"],
            ["Whipped Cream"],
            DateTime.UtcNow);
    }

    [Given("multiple recipe logs of different types have been ingested")]
    public async Task GivenMultipleRecipeLogsOfDifferentTypesHaveBeenIngested()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;

        using var scope = appManager.AppFactory!.Services.CreateScope();
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

        await ingester.IngestRecipeLogAsync(
            _recipeOrderId3,
            "Waffles",
            ["Milk", "Eggs", "Flour", "Butter"],
            ["Whipped Cream"],
            DateTime.UtcNow);
    }

    [When("the order summaries are queried via graphql")]
    public async Task WhenTheOrderSummariesAreQueriedViaGraphql()
        => await graphQlSteps.QueryOrderSummaries();

    [When("the recipe reports are queried via graphql")]
    public async Task WhenTheRecipeReportsAreQueriedViaGraphql()
        => await graphQlSteps.QueryRecipeReports();

    [When("the ingredient usage is queried via graphql")]
    public async Task WhenTheIngredientUsageIsQueriedViaGraphql()
        => await graphQlSteps.QueryIngredientUsage();

    [When("the popular recipes are queried via graphql")]
    public async Task WhenThePopularRecipesAreQueriedViaGraphql()
        => await graphQlSteps.QueryPopularRecipes();

    [Then("the graphql response should contain the ingested order summary")]
    public async Task ThenTheGraphqlResponseShouldContainTheIngestedOrderSummary()
    {
        Track.That(() => graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await graphQlSteps.ParseOrderSummariesResponse();
        Track.That(() => graphQlSteps.OrderSummaries.Should().Contain(o =>
            o.OrderId == _testOrderId &&
            o.CustomerName == _customerName &&
            o.ItemCount == 3 &&
            o.TableNumber == 7));
    }

    [Then("the graphql response should be successful")]
    public void ThenTheGraphqlResponseShouldBeSuccessful()
        => Track.That(() => graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    [Then("the order summaries list should be empty or not contain the test order")]
    public async Task ThenTheOrderSummariesListShouldBeEmptyOrNotContainTheTestOrder()
    {
        await graphQlSteps.ParseOrderSummariesResponse();
        Track.That(() => graphQlSteps.OrderSummaries.Should().NotContain(o => o.OrderId == _testOrderId));
    }

    [Then("the graphql response should contain the ingested recipe reports")]
    public async Task ThenTheGraphqlResponseShouldContainTheIngestedRecipeReports()
    {
        Track.That(() => graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await graphQlSteps.ParseRecipeReportsResponse();
        Track.That(() => graphQlSteps.RecipeReports.Should().Contain(r =>
            r.OrderId == _recipeOrderId1 &&
            r.RecipeType == "Pancakes" &&
            r.Ingredients.Contains("Milk")));
    }

    [Then("the ingredient usage should reflect aggregated counts")]
    public async Task ThenTheIngredientUsageShouldReflectAggregatedCounts()
    {
        Track.That(() => graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await graphQlSteps.ParseIngredientUsageResponse();
        Track.That(() => graphQlSteps.IngredientUsage.Should().Contain(i => i.Ingredient == "Milk" && i.Count >= 2));
        Track.That(() => graphQlSteps.IngredientUsage.Should().Contain(i => i.Ingredient == "Butter" && i.Count >= 1));
    }

    [Then("the popular recipes should be ordered by count descending")]
    public async Task ThenThePopularRecipesShouldBeOrderedByCountDescending()
    {
        Track.That(() => graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await graphQlSteps.ParsePopularRecipesResponse();
        var pancakes = graphQlSteps.PopularRecipes!.FirstOrDefault(r => r.RecipeType == "Pancakes");
        Track.That(() => pancakes.Should().NotBeNull());
        Track.That(() => pancakes!.Count.Should().BeGreaterThanOrEqualTo(2));
        Track.That(() => graphQlSteps.PopularRecipes.Should().Contain(r => r.RecipeType == "Waffles" && r.Count >= 1));
    }
}
