using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeCosts;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.RecipeCosts;

public partial class Recipe_Costs__Analysis_Feature : BaseFixture
{
    private readonly PublishRecipeCostEventSteps _publishSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Recipe_Costs__Analysis_Feature()
    {
        _publishSteps = Get<PublishRecipeCostEventSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

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

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_kitchen_service_should_have_received_the_preparation_request()
    {
        _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
        await Task.CompletedTask;
    }
}
