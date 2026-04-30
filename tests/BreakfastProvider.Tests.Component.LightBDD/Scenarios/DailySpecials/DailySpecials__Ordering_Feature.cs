using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.DailySpecials;

[FeatureDescription($"/{Endpoints.DailySpecials} - Ordering daily specials with threshold limits")]
public partial class DailySpecials__Ordering_Feature
{
    [HappyPath]
    [Scenario]
    public async Task A_Valid_Daily_Special_Order_Should_Return_A_Confirmation()
    {
        await Runner.RunScenarioAsync(
            given => The_cinnamon_swirl_order_count_is_reset(),
            and => A_valid_daily_special_order_request_for_cinnamon_swirl(),
            when => The_daily_special_order_is_submitted(),
            then => The_daily_special_order_response_should_contain_a_valid_confirmation());
    }

    [Scenario]
    public async Task The_Daily_Specials_Endpoint_Should_Return_All_Available_Specials()
    {
        await Runner.RunScenarioAsync(
            when => The_available_daily_specials_are_requested(),
            then => The_daily_specials_response_should_contain_all_expected_specials());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsNonDefaultConfiguration)]
    public async Task Ordering_A_Daily_Special_Beyond_The_Threshold_Should_Return_A_Conflict_Response()
    {
        await Runner.RunScenarioAsync(
            given => The_matcha_waffles_order_count_is_reset(),
            and => The_matcha_waffles_special_has_been_ordered_up_to_the_configured_limit(),
            when => Another_order_is_placed_for_the_matcha_waffles_special(),
            then => The_response_should_indicate_the_daily_special_is_sold_out());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsNonDefaultConfiguration)]
    public async Task Remaining_Quantity_Should_Decrease_After_Each_Order()
    {
        await Runner.RunScenarioAsync(
            given => The_lemon_ricotta_order_count_is_reset(),
            and => A_daily_special_order_for_lemon_ricotta_of_quantity_one_is_placed(),
            when => The_available_daily_specials_are_requested(),
            then => The_lemon_ricotta_special_should_have_one_fewer_remaining());
    }
}
