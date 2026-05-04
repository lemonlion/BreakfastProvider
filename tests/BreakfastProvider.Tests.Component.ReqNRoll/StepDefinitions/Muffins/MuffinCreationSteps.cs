using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Muffins;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Muffins;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Muffins;

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
        Track.That(() => milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        muffinSteps.Request.Milk = milkSteps.MilkResponse.Milk;

        await eggsSteps.Retrieve();
        Track.That(() => eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        muffinSteps.Request.Eggs = eggsSteps.EggsResponse.Eggs;

        await flourSteps.Retrieve();
        Track.That(() => flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
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
        Track.That(() => milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        muffinSteps.Request.Milk = milkSteps.MilkResponse.Milk;

        await eggsSteps.Retrieve();
        Track.That(() => eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
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
        Track.That(() => muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await muffinSteps.ParseResponse();
        Track.That(() => muffinSteps.Response!.Ingredients.Should().Contain(milkSteps.MilkResponse.Milk));
        Track.That(() => muffinSteps.Response!.Ingredients.Should().Contain(eggsSteps.EggsResponse.Eggs));
        Track.That(() => muffinSteps.Response!.Ingredients.Should().Contain(flourSteps.FlourResponse.Flour));
        Track.That(() => muffinSteps.Response!.Ingredients.Should().Contain(MuffinDefaults.GrannySmithApples));
        Track.That(() => muffinSteps.Response!.Ingredients.Should().Contain(MuffinDefaults.CeylonCinnamon));
        Track.That(() => muffinSteps.Response!.Toppings.Should().HaveCount(1));
        Track.That(() => muffinSteps.Response!.BakingTemperature.Should().Be(MuffinDefaults.DefaultTemperature));
        Track.That(() => muffinSteps.Response!.BakingDuration.Should().Be(MuffinDefaults.DefaultDuration));
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
        Track.That(() => muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await muffinSteps.ParseResponse();
        Track.That(() => muffinSteps.Response!.Ingredients.Should().HaveCount(expectedCount));
    }

    [Then(@"the muffin response should include (\d+) toppings")]
    public void ThenTheMuffinResponseShouldIncludeToppings(int expectedCount)
    {
        Track.That(() => muffinSteps.Response!.Toppings.Should().HaveCount(expectedCount));
    }

    [Then(@"the muffin response should contain error ""(.*)"" with status ""(.*)""")]
    public async Task ThenTheMuffinResponseShouldContainError(string errorMessage, string responseStatus)
    {
        var responseBody = await muffinSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => responseBody.Should().Contain(errorMessage));
        Track.That(() => muffinSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
    }
}
