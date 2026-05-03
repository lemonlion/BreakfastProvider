using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

#pragma warning disable CS1998
public partial class Toppings__Feature_Flag_Feature : BaseFixture
{
    private GetToppingsSteps _toppingsSteps = null!;

    public Toppings__Feature_Flag_Feature() : base(delayAppCreation: true)
    {
    }

    private void EnsureAppCreated(Dictionary<string, string?> overrides)
    {
        CreateAppAndClient(overrides);
        _toppingsSteps = Get<GetToppingsSteps>();
    }

    #region Given

    private async Task The_raspberry_topping_feature_flag_is_disabled()
        => EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsRaspberryToppingEnabled)}"] = "false"
        });

    private async Task The_raspberry_topping_feature_flag_is_enabled()
        => EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(FeatureSwitchesConfig)}:{nameof(FeatureSwitchesConfig.IsRaspberryToppingEnabled)}"] = "true"
        });

    #endregion

    #region When

    private async Task Toppings_are_requested()
        => await _toppingsSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_toppings_response_should_not_include_raspberries()
    {
        return Sub.Steps(
            _ => The_toppings_response_http_status_should_be_ok(),
            _ => The_toppings_list_should_be_valid_json(),
            _ => The_toppings_list_should_not_contain_raspberries());
    }

    private async Task<CompositeStep> The_toppings_response_should_include_raspberries()
    {
        return Sub.Steps(
            _ => The_toppings_response_http_status_should_be_ok(),
            _ => The_toppings_list_should_be_valid_json(),
            _ => The_toppings_list_should_contain_raspberries());
    }

    private async Task The_toppings_response_http_status_should_be_ok()
        => Track.That(() => _toppingsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_toppings_list_should_be_valid_json()
        => await _toppingsSteps.ParseResponse();

    private async Task The_toppings_list_should_not_contain_raspberries()
        => Track.That(() => _toppingsSteps.Response!.Should().NotContain(t => t.Name == ToppingDefaults.Raspberries));

    private async Task The_toppings_list_should_contain_raspberries()
        => Track.That(() => _toppingsSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries));

    #endregion
}
