using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeCosts;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeCosts;
using BreakfastProvider.Tests.Component.NUnit.Infrastructure;
using TestTrackingDiagrams.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.RecipeCosts;

public class RecipeCost_Analysis_Tests : BaseFixture
{
    private readonly PostRecipeCostSteps _postSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public RecipeCost_Analysis_Tests()
    {
        _postSteps = Get<PostRecipeCostSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Submitting_recipe_cost_should_trigger_event_consumption_and_downstream_calls()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given a valid recipe cost calculation request
        _postSteps.Request = new TestRecipeCostRequest
        {
            RecipeName = $"Recipe-{Guid.NewGuid():N}",
            Ingredients = ["flour", "eggs", "milk", "sugar"],
            TotalCost = 4.99m,
            Currency = "GBP"
        };

        // When the cost calculation is submitted (triggers Kafka event → consumer → BigQuery + gRPC + HTTP)
        await _postSteps.Send();

        // Then the response should be accepted
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await _postSteps.ParseResponse();
        _postSteps.Response!.CalculationId.Should().NotBe(Guid.Empty);

        // And the kitchen service should have received the preparation request
        await Task.Delay(500); // Allow async consumer processing
        _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
    }
}
