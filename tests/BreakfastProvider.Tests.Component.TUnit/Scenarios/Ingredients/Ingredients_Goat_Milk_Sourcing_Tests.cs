using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Ingredients;

public class Ingredients_Goat_Milk_Sourcing_Tests : BaseFixture
{
    private readonly GetGoatMilkSteps _goatMilkSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Ingredients_Goat_Milk_Sourcing_Tests()
    {
        _goatMilkSteps = Get<GetGoatMilkSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Goat_milk_endpoint_should_return_fresh_goat_milk_from_goat_service()
    {
        // When goat milk is requested
        await _goatMilkSteps.Retrieve();

        // Then the goat milk response should contain fresh goat milk
        _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _goatMilkSteps.GoatMilkResponse.Should().NotBeNull();
        _goatMilkSteps.GoatMilkResponse.GoatMilk.Should().Be(GoatServiceDefaults.FreshGoatMilk);

        // And the goat service should have received a goat milk request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertGoatServiceReceivedGoatMilkRequest();
    }
}
