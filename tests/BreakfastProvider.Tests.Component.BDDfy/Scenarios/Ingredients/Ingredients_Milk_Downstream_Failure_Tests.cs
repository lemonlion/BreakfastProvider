using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Ingredients;

public class Ingredients_Milk_Downstream_Failure_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;

    public Ingredients_Milk_Downstream_Failure_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
    }

    [Fact]
    public void Requesting_milk_when_cow_service_unavailable_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_cow_service_will_return_service_unavailable())
            .When(x => x.Milk_is_requested())
            .Then(x => x.The_milk_response_should_indicate_a_bad_gateway_with_unavailable_message())
            .BDDfy();
    }

    [Fact]
    public void Requesting_milk_when_cow_service_times_out_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_cow_service_will_return_a_timeout())
            .When(x => x.Milk_is_requested())
            .Then(x => x.The_milk_response_should_indicate_a_bad_gateway_with_unavailable_message())
            .BDDfy();
    }

    [Fact]
    public void Requesting_milk_when_cow_service_returns_invalid_response_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_cow_service_will_return_an_invalid_response())
            .When(x => x.Milk_is_requested())
            .Then(x => x.The_milk_response_should_indicate_a_bad_gateway_with_unavailable_message())
            .BDDfy();
    }

    #region Steps

    private void The_cow_service_will_return_service_unavailable()
    {
        _milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.ServiceUnavailable);
    }

    private void The_cow_service_will_return_a_timeout()
    {
        _milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.Timeout);
    }

    private void The_cow_service_will_return_an_invalid_response()
    {
        _milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.InvalidResponse);
    }

    private async Task Milk_is_requested()
    {
        await _milkSteps.Retrieve();
    }

    private async Task The_milk_response_should_indicate_a_bad_gateway_with_unavailable_message()
    {
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        var milkErrorResponseBody = await _milkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        milkErrorResponseBody.Should().Contain(DownstreamErrorMessages.CowServiceUnavailableTitle);
    }

    #endregion
}
