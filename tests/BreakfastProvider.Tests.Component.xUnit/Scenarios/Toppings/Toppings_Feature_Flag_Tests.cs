using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Toppings;

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

    [Fact]
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
        Track.That(() => _toppingsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _toppingsSteps.ParseResponse();
        Track.That(() => _toppingsSteps.Response!.Should().NotContain(t => t.Name == ToppingDefaults.Raspberries));
    }

    [Fact]
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
        Track.That(() => _toppingsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _toppingsSteps.ParseResponse();
        Track.That(() => _toppingsSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries));
    }
}
