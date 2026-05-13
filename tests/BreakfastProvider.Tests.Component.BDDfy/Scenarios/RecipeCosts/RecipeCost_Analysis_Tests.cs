using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeCosts;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.RecipeCosts;

public class RecipeCost_Analysis_Tests : BaseFixture
{
    private readonly PostRecipeCostSteps _postSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public RecipeCost_Analysis_Tests()
    {
        _postSteps = Get<PostRecipeCostSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public void Submitting_recipe_cost_should_trigger_event_consumption_and_downstream_calls()
    {
        this.Given(x => x.A_valid_recipe_cost_request_is_prepared())
            .When(x => x.The_cost_calculation_is_submitted())
            .Then(x => x.The_response_should_be_accepted())
            .And(x => x.The_kitchen_service_should_have_received_the_preparation_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_recipe_cost_request_is_prepared()
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

    #endregion
}
