using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Ingredients;

public class Ingredients_Goat_Milk_Sourcing_Tests : BaseFixture
{
    private readonly GetGoatMilkSteps _goatMilkSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Ingredients_Goat_Milk_Sourcing_Tests()
    {
        _goatMilkSteps = Get<GetGoatMilkSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public void Goat_milk_endpoint_should_return_fresh_goat_milk_from_goat_service()
    {
        this.When(x => x.Goat_milk_is_requested())
            .Then(x => x.The_response_should_contain_fresh_goat_milk())
            .And(x => x.The_goat_service_should_have_received_a_goat_milk_request())
            .BDDfy();
    }

    #region Steps

    private async Task Goat_milk_is_requested()
    {
        await _goatMilkSteps.Retrieve();
    }

    private void The_response_should_contain_fresh_goat_milk()
    {
        _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _goatMilkSteps.GoatMilkResponse.Should().NotBeNull();
        _goatMilkSteps.GoatMilkResponse.GoatMilk.Should().Be(GoatServiceDefaults.FreshGoatMilk);
    }

    private void The_goat_service_should_have_received_a_goat_milk_request()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertGoatServiceReceivedGoatMilkRequest();
    }

    #endregion
}
