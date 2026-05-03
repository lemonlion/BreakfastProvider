using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Ingredients;

public class Ingredients_Goat_Milk_Feature_Flag_Tests : BaseFixture
{
    private GetGoatMilkSteps _goatMilkSteps = null!;

    public Ingredients_Goat_Milk_Feature_Flag_Tests() : base(delayAppCreation: true)
    {
    }

    private void EnsureAppCreated(Dictionary<string, string?> overrides)
    {
        CreateAppAndClient(overrides);
        _goatMilkSteps = Get<GetGoatMilkSteps>();
    }

    [Fact]
    public async Task Goat_milk_endpoint_should_return_not_found_when_feature_is_disabled()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the goat milk feature flag is disabled
        EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsGoatMilkEnabled)}"] = "false"
        });

        // When goat milk is requested
        await _goatMilkSteps.Retrieve();

        // Then the goat milk response should indicate feature disabled
        Track.That(() => _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));
        var goatMilkFeatureDisabledResponseBody = await _goatMilkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => goatMilkFeatureDisabledResponseBody.Should().Contain(DownstreamErrorMessages.FeatureDisabled));
    }

    [Fact]
    public async Task Goat_milk_endpoint_should_return_fresh_goat_milk_when_feature_is_enabled()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the goat milk feature flag is enabled
        EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsGoatMilkEnabled)}"] = "true"
        });

        // When goat milk is requested
        await _goatMilkSteps.Retrieve();

        // Then the goat milk response should contain fresh goat milk
        Track.That(() => _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        Track.That(() => _goatMilkSteps.GoatMilkResponse.GoatMilk.Should().Be(GoatServiceDefaults.FreshGoatMilk));
    }
}
