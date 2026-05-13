using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeCosts;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.RecipeCosts;

public partial class Recipe_Costs__Analysis_Feature : BaseFixture
{
    private readonly PostRecipeCostSteps _postSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Recipe_Costs__Analysis_Feature()
    {
        _postSteps = Get<PostRecipeCostSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    private async Task A_valid_recipe_cost_request()
    {
        _postSteps.Request = new TestRecipeCostRequest
        {
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Ingredients = ["flour", "eggs", "milk", "sugar"],
            TotalCost = 4.99m,
            Currency = "GBP"
        };
        await Task.CompletedTask;
    }

    private async Task The_cost_calculation_is_submitted() => await _postSteps.Send();

    private async Task The_response_should_be_accepted()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await _postSteps.ParseResponse();
        _postSteps.Response!.CalculationId.Should().NotBe(Guid.Empty);
    }

    private async Task The_kitchen_service_should_have_received_the_preparation_request()
    {
        await Task.Delay(500); // Allow async consumer processing
        _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
    }
}
