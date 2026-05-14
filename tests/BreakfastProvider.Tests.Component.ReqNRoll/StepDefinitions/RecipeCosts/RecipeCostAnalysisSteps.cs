using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeCosts;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.RecipeCosts;

[Binding]
public class RecipeCostAnalysisSteps(
    PublishRecipeCostEventSteps publishSteps,
    DownstreamRequestSteps downstreamSteps)
{
    [Given("a recipe cost calculated event")]
    public void GivenARecipeCostCalculatedEvent()
    {
        publishSteps.Request = new TestRecipeCostRequest
        {
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Ingredients = ["flour", "eggs", "milk", "sugar"],
            TotalCost = 4.99m,
            Currency = "GBP"
        };
    }

    [When("the event is published to Kafka")]
    public async Task WhenTheEventIsPublishedToKafka()
    {
        await publishSteps.PublishEvent();
    }

    [Then("the calculation ID should be generated")]
    public void ThenTheCalculationIdShouldBeGenerated()
    {
        publishSteps.CalculationId.Should().NotBe(Guid.Empty);
    }

    [Then("the kitchen service should have received the preparation request")]
    public void ThenTheKitchenServiceShouldHaveReceivedThePreparationRequest()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;
        downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
    }
}
