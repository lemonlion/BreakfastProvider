using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Rate limiting on order creation")]
[IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), IgnoreReasons.NeedsNonDefaultConfiguration)]
public partial class Orders__Rate_Limiting_Feature
{
    [Scenario]
    public async Task Exceeding_The_Rate_Limit_Should_Return_Too_Many_Requests()
    {
        await Runner.RunScenarioAsync(
            given => The_rate_limit_is_configured_to_allow_one_request_per_window(),
            and => A_pancake_batch_has_been_created(),
            and => A_valid_order_request(),
            when => The_order_is_submitted_twice_in_rapid_succession(),
            then => The_first_request_should_succeed(),
            and => The_second_request_should_be_rate_limited());
    }
}
