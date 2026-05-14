using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeCosts;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.RecipeCosts;

public class RecipeCost_Analysis_Tests : BaseFixture
{
    private readonly PublishRecipeCostEventSteps _publishSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public RecipeCost_Analysis_Tests()
    {
        _publishSteps = Get<PublishRecipeCostEventSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Consuming_recipe_cost_event_should_trigger_downstream_processing()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given a recipe cost calculated event
        _publishSteps.Request = new TestRecipeCostRequest
        {
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Ingredients = ["flour", "eggs", "milk", "sugar"],
            TotalCost = 4.99m,
            Currency = "GBP"
        };

        // When the event is published to Kafka (consumed by BreakfastProvider → BigQuery + gRPC + HTTP)
        await _publishSteps.PublishEvent();

        // Then the calculation ID should be generated
        _publishSteps.CalculationId.Should().NotBe(Guid.Empty);

        // And the kitchen service should have received the preparation request
        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
        }
    }
}
