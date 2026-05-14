using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeCosts;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.RecipeCosts;

[Binding]
public class RecipeCostAnalysisSteps(
    PostRecipeCostSteps postSteps,
    DownstreamRequestSteps downstreamSteps)
{
    [Given("a valid recipe cost calculation request")]
    public void GivenAValidRecipeCostCalculationRequest()
    {
        postSteps.Request = new TestRecipeCostRequest
        {
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Ingredients = ["flour", "eggs", "milk", "sugar"],
            TotalCost = 4.99m,
            Currency = "GBP"
        };
    }

    [When("the recipe cost calculation is submitted")]
    public async Task WhenTheRecipeCostCalculationIsSubmitted()
    {
        await postSteps.Send();
    }

    [Then("the cost response should be accepted")]
    public async Task ThenTheCostResponseShouldBeAccepted()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await postSteps.ParseResponse();
        postSteps.Response!.CalculationId.Should().NotBe(Guid.Empty);
    }

    [Then("the kitchen service should have received the preparation request")]
    public async Task ThenTheKitchenServiceShouldHaveReceivedThePreparationRequest()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;
        await Task.Delay(500); // Allow async consumer processing
        downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
    }
}
