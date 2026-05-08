using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Toppings;

#pragma warning disable CS1998
public class Toppings_Feature_Flag_Tests : BaseFixture
{
    private GetToppingsSteps _toppingsSteps = null!;

    public Toppings_Feature_Flag_Tests() : base(delayAppCreation: true)
    {
    }

    private void EnsureAppCreated(Dictionary<string, string?> overrides)
    {
        CreateAppAndClient(overrides);
        _toppingsSteps = Get<GetToppingsSteps>();
    }

    [Test]
    public async Task Toppings_should_exclude_raspberries_when_feature_flag_is_disabled()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the raspberry topping feature flag is disabled
        EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsRaspberryToppingEnabled)}"] = "false"
        });

        // When toppings are requested
        await _toppingsSteps.Retrieve();

        // Then the toppings response should not include raspberries
        await _toppingsSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _toppingsSteps.ParseResponse();
        await _toppingsSteps.Response!.Should().All(t => t.Name != ToppingDefaults.Raspberries);
    }

    [Test]
    public async Task Toppings_should_include_raspberries_when_feature_flag_is_enabled()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the raspberry topping feature flag is enabled
        EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsRaspberryToppingEnabled)}"] = "true"
        });

        // When toppings are requested
        await _toppingsSteps.Retrieve();

        // Then the toppings response should include raspberries
        await _toppingsSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _toppingsSteps.ParseResponse();
        await _toppingsSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries);
    }
}
