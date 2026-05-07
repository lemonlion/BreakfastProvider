using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Toppings;

public class Toppings_Feature_Flag_Tests : BaseFixture
{
    private GetToppingsSteps _toppingsSteps = null!;

    public Toppings_Feature_Flag_Tests() : base(delayAppCreation: true)
    {
    }

    [Fact]
    public void Toppings_should_exclude_raspberries_when_feature_flag_is_disabled()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_raspberry_topping_feature_flag_is_disabled())
            .When(x => x.Toppings_are_requested())
            .Then(x => x.The_toppings_response_should_not_include_raspberries())
            .BDDfy();
    }

    [Fact]
    public void Toppings_should_include_raspberries_when_feature_flag_is_enabled()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_raspberry_topping_feature_flag_is_enabled())
            .When(x => x.Toppings_are_requested())
            .Then(x => x.The_toppings_response_should_include_raspberries())
            .BDDfy();
    }

    #region Steps

    private void The_raspberry_topping_feature_flag_is_disabled()
    {
        CreateAppAndClient(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsRaspberryToppingEnabled)}"] = "false"
        });
        _toppingsSteps = Get<GetToppingsSteps>();
    }

    private void The_raspberry_topping_feature_flag_is_enabled()
    {
        CreateAppAndClient(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsRaspberryToppingEnabled)}"] = "true"
        });
        _toppingsSteps = Get<GetToppingsSteps>();
    }

    private async Task Toppings_are_requested()
    {
        await _toppingsSteps.Retrieve();
    }

    private async Task The_toppings_response_should_not_include_raspberries()
    {
        _toppingsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _toppingsSteps.ParseResponse();
        _toppingsSteps.Response!.Should().NotContain(t => t.Name == ToppingDefaults.Raspberries);
    }

    private async Task The_toppings_response_should_include_raspberries()
    {
        _toppingsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _toppingsSteps.ParseResponse();
        _toppingsSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries);
    }

    #endregion
}
