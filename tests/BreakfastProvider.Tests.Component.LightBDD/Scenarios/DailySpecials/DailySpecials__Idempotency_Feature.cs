using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.DailySpecials;

[FeatureDescription($"/{Endpoints.DailySpecialsOrders} - Idempotent order creation using Idempotency-Key header")]
public partial class DailySpecials__Idempotency_Feature
{
    [Scenario]
    public async Task Submitting_The_Same_Order_With_The_Same_Idempotency_Key_Should_Return_The_Same_Confirmation()
    {
        await Runner.RunScenarioAsync(
            given => The_cinnamon_swirl_order_count_is_reset(),
            and => An_order_request_with_an_idempotency_key(),
            when => The_order_is_submitted_twice_with_the_same_idempotency_key(),
            then => Both_responses_should_return_the_same_confirmation_id());
    }

    [Scenario]
    public async Task Submitting_The_Same_Order_With_Different_Idempotency_Keys_Should_Return_Different_Confirmations()
    {
        await Runner.RunScenarioAsync(
            given => The_cinnamon_swirl_order_count_is_reset(),
            and => An_order_request_for_the_same_special(),
            when => The_order_is_submitted_with_two_different_idempotency_keys(),
            then => The_responses_should_have_different_confirmation_ids());
    }
}
