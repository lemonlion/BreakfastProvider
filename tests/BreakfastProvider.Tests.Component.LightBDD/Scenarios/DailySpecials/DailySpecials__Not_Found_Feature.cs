using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.DailySpecials;

[FeatureDescription($"/{Endpoints.DailySpecialsOrders} - Ordering a non-existent daily special")]
public partial class DailySpecials__Not_Found_Feature
{
    [Scenario]
    public async Task Ordering_A_Non_Existent_Daily_Special_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            given => A_daily_special_order_request_for_a_non_existent_special(),
            when => The_daily_special_order_is_submitted(),
            then => The_response_should_indicate_not_found());
    }
}
