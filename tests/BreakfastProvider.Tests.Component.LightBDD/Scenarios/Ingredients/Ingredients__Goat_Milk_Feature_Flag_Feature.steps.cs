using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Ingredients;

#pragma warning disable CS1998
public partial class Ingredients__Goat_Milk_Feature_Flag_Feature : BaseFixture
{
    private GetGoatMilkSteps _goatMilkSteps = null!;

    public Ingredients__Goat_Milk_Feature_Flag_Feature() : base(delayAppCreation: true)
    {
    }

    private void EnsureAppCreated(Dictionary<string, string?> overrides)
    {
        CreateAppAndClient(overrides);
        _goatMilkSteps = Get<GetGoatMilkSteps>();
    }

    #region Given

    private async Task The_goat_milk_feature_flag_is_disabled()
        => EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsGoatMilkEnabled)}"] = "false"
        });

    private async Task The_goat_milk_feature_flag_is_enabled()
        => EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsGoatMilkEnabled)}"] = "true"
        });

    #endregion

    #region When

    private async Task Goat_milk_is_requested()
        => await _goatMilkSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_goat_milk_response_should_indicate_feature_disabled()
    {
        return Sub.Steps(
            _ => The_goat_milk_response_http_status_should_be_not_found(),
            _ => The_goat_milk_error_should_indicate_feature_disabled());
    }

    private async Task The_goat_milk_response_http_status_should_be_not_found()
        => _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    private async Task The_goat_milk_error_should_indicate_feature_disabled()
    {
        var content = await _goatMilkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        content.Should().Contain(DownstreamErrorMessages.FeatureDisabled);
    }

    private async Task<CompositeStep> The_goat_milk_response_should_contain_fresh_goat_milk()
    {
        return Sub.Steps(
            _ => The_goat_milk_response_http_status_should_be_ok(),
            _ => The_goat_milk_should_be_fresh());
    }

    private async Task The_goat_milk_response_http_status_should_be_ok()
        => _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_goat_milk_should_be_fresh()
        => _goatMilkSteps.GoatMilkResponse.GoatMilk.Should().Be(GoatServiceDefaults.FreshGoatMilk);

    #endregion
}
