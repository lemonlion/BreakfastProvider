using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

[FeatureDescription($"/{Endpoints.Toppings} - Topping availability controlled by feature flags")]
public partial class Toppings__Feature_Flag_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsNonDefaultConfiguration)]
    public async Task Toppings_Should_Exclude_Raspberries_When_Feature_Flag_Is_Disabled()
    {
        await Runner.RunScenarioAsync(
            given => The_raspberry_topping_feature_flag_is_disabled(),
            when => Toppings_are_requested(),
            then => The_toppings_response_should_not_include_raspberries());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsNonDefaultConfiguration)]
    public async Task Toppings_Should_Include_Raspberries_When_Feature_Flag_Is_Enabled()
    {
        await Runner.RunScenarioAsync(
            given => The_raspberry_topping_feature_flag_is_enabled(),
            when => Toppings_are_requested(),
            then => The_toppings_response_should_include_raspberries());
    }
}
