using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeCosts;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.RecipeCosts;

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
    public void Consuming_recipe_cost_event_should_trigger_downstream_processing()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.A_recipe_cost_calculated_event())
            .When(x => x.The_event_is_published_to_kafka())
            .Then(x => x.The_calculation_id_should_be_generated())
            .And(x => x.The_kitchen_service_should_have_received_the_preparation_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_recipe_cost_calculated_event()
    {
        _publishSteps.Request = new TestRecipeCostRequest
        {
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Ingredients = ["flour", "eggs", "milk", "sugar"],
            TotalCost = 4.99m,
            Currency = "GBP"
        };
        await Task.CompletedTask;
    }

    private async Task The_event_is_published_to_kafka() => await _publishSteps.PublishEvent();

    private async Task The_calculation_id_should_be_generated()
    {
        _publishSteps.CalculationId.Should().NotBe(Guid.Empty);
        await Task.CompletedTask;
    }

    private async Task The_kitchen_service_should_have_received_the_preparation_request()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;
        _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
        await Task.CompletedTask;
    }

    #endregion
}
