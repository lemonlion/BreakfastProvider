using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.AppleCinnamonMuffins;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.AppleCinnamonMuffins;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.AppleCinnamonMuffins;

[Binding]
public class MuffinCreationSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostMuffinsSteps muffinSteps,
    DownstreamRequestSteps downstreamSteps)
{
    private readonly List<HttpResponseMessage> _validationResponses = [];

    [Given("a valid apple cinnamon muffin recipe with all ingredients")]
    public async Task GivenAValidAppleCinnamonMuffinRecipeWithAllIngredients()
    {
        await milkSteps.Retrieve();
        milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        muffinSteps.Request.Milk = milkSteps.MilkResponse.Milk;

        await eggsSteps.Retrieve();
        eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        muffinSteps.Request.Eggs = eggsSteps.EggsResponse.Eggs;

        await flourSteps.Retrieve();
        flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        muffinSteps.Request.Flour = flourSteps.FlourResponse.Flour;

        muffinSteps.Request.Apples = MuffinDefaults.GrannySmithApples;
        muffinSteps.Request.Cinnamon = MuffinDefaults.CeylonCinnamon;
        muffinSteps.Request.Baking = new TestBakingProfile
        {
            Temperature = MuffinDefaults.DefaultTemperature,
            DurationMinutes = MuffinDefaults.DefaultDuration,
            PanType = MuffinDefaults.DefaultPanType
        };
        muffinSteps.Request.Toppings = [new TestMuffinTopping { Name = "Streusel", Amount = "Light" }];
    }

    [Given(@"a muffin recipe ""(.*)"" with the following ingredients:")]
    public async Task GivenAMuffinRecipeWithIngredients(string recipeName, Table table)
    {
        await milkSteps.Retrieve();
        milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        muffinSteps.Request.Milk = milkSteps.MilkResponse.Milk;

        await eggsSteps.Retrieve();
        eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        muffinSteps.Request.Eggs = eggsSteps.EggsResponse.Eggs;

        var row = table.Rows[0];
        muffinSteps.Request.Flour = row["Flour"];
        muffinSteps.Request.Apples = row["Apples"];
        muffinSteps.Request.Cinnamon = row["Cinnamon"];
    }

    [Given(@"with baking at (\d+) degrees for (\d+) minutes in a ""(.*)"" pan")]
    public void GivenWithBaking(int temperature, int duration, string panType)
    {
        muffinSteps.Request.Baking = new TestBakingProfile
        {
            Temperature = temperature,
            DurationMinutes = duration,
            PanType = panType
        };
    }

    [Given("the following muffin toppings:")]
    public void GivenTheFollowingMuffinToppings(Table table)
    {
        muffinSteps.Request.Toppings = table.Rows
            .Select(row => new TestMuffinTopping { Name = row["Name"], Amount = row["Amount"] })
            .ToList();
    }

    [Given("no muffin toppings")]
    public void GivenNoMuffinToppings()
    {
        muffinSteps.Request.Toppings = null;
    }

    [Given(@"a valid muffin request with ""(.*)"" set to ""(.*)""")]
    public void GivenAValidMuffinRequestWithFieldSetToValue(string field, string value)
    {
        var validBase = new TestMuffinRequest
        {
            Milk = CowServiceDefaults.FreshMilk,
            Flour = IngredientDefaults.PlainFlour,
            Eggs = IngredientDefaults.FreeRangeEggs,
            Apples = MuffinDefaults.GrannySmithApples,
            Cinnamon = MuffinDefaults.CeylonCinnamon,
            Baking = new TestBakingProfile
            {
                Temperature = MuffinDefaults.DefaultTemperature,
                DurationMinutes = MuffinDefaults.DefaultDuration,
                PanType = MuffinDefaults.DefaultPanType
            },
            Toppings = [new TestMuffinTopping { Name = "Streusel", Amount = "Light" }]
        };

        var input = new InvalidFieldFromRequest(field, value, string.Empty);
        var requests = ValidationHelper.CreateValidationRequests(validBase, [input]);
        muffinSteps.Request = requests.Single();
    }

    [When("the muffins are prepared")]
    public async Task WhenTheMuffinsArePrepared()
    {
        await muffinSteps.Send();
    }

    [When("the invalid muffin request is submitted")]
    public async Task WhenTheInvalidMuffinRequestIsSubmitted()
    {
        await muffinSteps.Send();
    }

    [Then("the muffin response should contain a valid batch with all ingredients")]
    public async Task ThenTheMuffinResponseShouldContainAValidBatchWithAllIngredients()
    {
        muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await muffinSteps.ParseResponse();
        muffinSteps.Response!.Ingredients.Should().Contain(milkSteps.MilkResponse.Milk);
        muffinSteps.Response!.Ingredients.Should().Contain(eggsSteps.EggsResponse.Eggs);
        muffinSteps.Response!.Ingredients.Should().Contain(flourSteps.FlourResponse.Flour);
        muffinSteps.Response!.Ingredients.Should().Contain(MuffinDefaults.GrannySmithApples);
        muffinSteps.Response!.Ingredients.Should().Contain(MuffinDefaults.CeylonCinnamon);
        muffinSteps.Response!.Toppings.Should().HaveCount(1);
        muffinSteps.Response!.BakingTemperature.Should().Be(MuffinDefaults.DefaultTemperature);
        muffinSteps.Response!.BakingDuration.Should().Be(MuffinDefaults.DefaultDuration);
    }

    [Then("the cow service should have received a milk request for the muffins")]
    public void ThenTheCowServiceShouldHaveReceivedAMilkRequest()
    {
        if (!AppManager.Settings.RunAgainstExternalServiceUnderTest)
        {
            downstreamSteps.AssertCowServiceReceivedMilkRequest();
        }
    }

    [Then(@"the muffin batch should have (\d+) ingredients")]
    public async Task ThenTheMuffinBatchShouldHaveIngredients(int expectedCount)
    {
        muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await muffinSteps.ParseResponse();
        muffinSteps.Response!.Ingredients.Should().HaveCount(expectedCount);
    }

    [Then(@"the muffin response should include (\d+) toppings")]
    public void ThenTheMuffinResponseShouldIncludeToppings(int expectedCount)
    {
        muffinSteps.Response!.Toppings.Should().HaveCount(expectedCount);
    }

    [Then("the muffin response should include baking information")]
    public void ThenTheMuffinResponseShouldIncludeBakingInformation()
    {
        muffinSteps.Response!.BakingTemperature.Should().BeGreaterThan(0);
    }

    [Then(@"the muffin response should contain error ""(.*)"" with status ""(.*)""")]
    public async Task ThenTheMuffinResponseShouldContainError(string errorMessage, string responseStatus)
    {
        var responseBody = await muffinSteps.ResponseMessage!.Content.ReadAsStringAsync();
        responseBody.Should().Contain(errorMessage);
        muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
