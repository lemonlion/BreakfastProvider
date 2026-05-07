using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Ingredients;

public class Ingredients_Goat_Milk_Downstream_Failure_Tests : BaseFixture
{
    private readonly GetGoatMilkSteps _goatMilkSteps;

    public Ingredients_Goat_Milk_Downstream_Failure_Tests()
    {
        _goatMilkSteps = Get<GetGoatMilkSteps>();
    }

    [Fact]
    public void Requesting_goat_milk_when_goat_service_unavailable_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_goat_service_will_return_service_unavailable())
            .When(x => x.Goat_milk_is_requested())
            .Then(x => x.The_goat_milk_response_should_indicate_a_bad_gateway())
            .BDDfy();
    }

    [Fact]
    public void Requesting_goat_milk_when_goat_service_returns_invalid_response_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_goat_service_will_return_an_invalid_response())
            .When(x => x.Goat_milk_is_requested())
            .Then(x => x.The_goat_milk_response_should_indicate_a_bad_gateway())
            .BDDfy();
    }

    #region Steps

    private void The_goat_service_will_return_service_unavailable()
    {
        _goatMilkSteps.AddHeader(FakeScenarioHeaders.GoatService, FakeScenarios.ServiceUnavailable);
    }

    private void The_goat_service_will_return_an_invalid_response()
    {
        _goatMilkSteps.AddHeader(FakeScenarioHeaders.GoatService, FakeScenarios.InvalidResponse);
    }

    private async Task Goat_milk_is_requested()
    {
        await _goatMilkSteps.Retrieve();
    }

    private async Task The_goat_milk_response_should_indicate_a_bad_gateway()
    {
        _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        var body = await _goatMilkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        body.Should().Contain(DownstreamErrorMessages.GoatServiceUnavailableTitle);
    }

    #endregion
}
