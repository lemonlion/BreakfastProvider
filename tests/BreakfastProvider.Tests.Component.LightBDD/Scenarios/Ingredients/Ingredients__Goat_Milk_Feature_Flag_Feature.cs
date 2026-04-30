using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Ingredients;

[FeatureDescription($"/{Endpoints.GoatMilk} - Goat milk availability controlled by feature flag")]
public partial class Ingredients__Goat_Milk_Feature_Flag_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsNonDefaultConfiguration)]
    public async Task Goat_Milk_Endpoint_Should_Return_Not_Found_When_Feature_Is_Disabled()
    {
        await Runner.RunScenarioAsync(
            given => The_goat_milk_feature_flag_is_disabled(),
            when => Goat_milk_is_requested(),
            then => The_goat_milk_response_should_indicate_feature_disabled());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsNonDefaultConfiguration)]
    public async Task Goat_Milk_Endpoint_Should_Return_Fresh_Goat_Milk_When_Feature_Is_Enabled()
    {
        await Runner.RunScenarioAsync(
            given => The_goat_milk_feature_flag_is_enabled(),
            when => Goat_milk_is_requested(),
            then => The_goat_milk_response_should_contain_fresh_goat_milk());
    }
}
