using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientUsage;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientUsage;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.IngredientUsage;

[Binding]
public class IngredientUsageAnalyticsSteps(
    AppManager appManager,
    PostIngredientUsageSteps postSteps,
    GetIngredientUsageSteps getSteps)
{
    private string _ingredientName = string.Empty;

    [Given("a valid ingredient usage request")]
    public void GivenAValidIngredientUsageRequest()
    {
        _ingredientName = $"Flour-{Guid.NewGuid():N}";
        postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = _ingredientName,
            QuantityUsed = 2.5m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };
    }

    [Given("an ingredient usage record has been created")]
    public async Task GivenAnIngredientUsageRecordHasBeenCreated()
    {
        GivenAValidIngredientUsageRequest();
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
    }

    [Given("an ingredient usage request with a missing ingredient name")]
    public void GivenAnIngredientUsageRequestWithAMissingIngredientName()
    {
        postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = null,
            QuantityUsed = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };
    }

    [Given("an ingredient usage request with zero quantity")]
    public void GivenAnIngredientUsageRequestWithZeroQuantity()
    {
        postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = "Eggs",
            QuantityUsed = 0,
            Unit = "units",
            RecipeName = "Pancakes"
        };
    }

    [When("the ingredient usage is recorded")]
    public async Task WhenTheIngredientUsageIsRecorded()
    {
        await postSteps.Send();
    }

    [When("the usage is listed by ingredient name")]
    public async Task WhenTheUsageIsListedByIngredientName()
    {
        await getSteps.RetrieveByIngredient(_ingredientName);
    }

    [When("the usage summary is requested")]
    public async Task WhenTheUsageSummaryIsRequested()
    {
        await getSteps.RetrieveSummary();
    }

    [Then("the usage response should contain the created record")]
    public async Task ThenTheUsageResponseShouldContainTheCreatedRecord()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.IngredientName.Should().Be(_ingredientName);
        postSteps.Response!.QuantityUsed.Should().Be(2.5m);
        postSteps.Response!.UsageId.Should().NotBeNullOrEmpty();
    }

    [Then("the usage list response should contain the record")]
    public async Task ThenTheUsageListResponseShouldContainTheRecord()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseListResponse();
        getSteps.ListResponse!.Should().Contain(u => u.IngredientName == _ingredientName);
    }

    [Then("the summary should contain aggregated data")]
    public async Task ThenTheSummaryShouldContainAggregatedData()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseSummaryResponse();
        getSteps.SummaryResponse!.Should().Contain(s => s.IngredientName == _ingredientName);
    }

    [Then("the usage post response should indicate bad request")]
    public void ThenTheUsagePostResponseShouldIndicateBadRequest()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
