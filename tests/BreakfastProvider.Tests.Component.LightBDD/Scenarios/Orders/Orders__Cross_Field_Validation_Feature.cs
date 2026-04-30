using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Cross-field validation with configurable item limits")]
[IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), IgnoreReasons.NeedsNonDefaultConfiguration)]
public partial class Orders__Cross_Field_Validation_Feature
{
    [Scenario]
    public async Task An_Order_Exceeding_The_Maximum_Items_Per_Order_Should_Be_Rejected()
    {
        await Runner.RunScenarioAsync(
            given => The_maximum_items_per_order_is_configured_to_two(),
            and => A_pancake_batch_has_been_created(),
            and => An_order_request_with_three_items(),
            when => The_order_is_submitted(),
            then => The_response_should_indicate_a_validation_error(),
            and => The_error_message_should_reference_the_item_limit());
    }

    [Scenario]
    public async Task An_Order_At_The_Maximum_Items_Per_Order_Should_Be_Accepted()
    {
        await Runner.RunScenarioAsync(
            given => The_maximum_items_per_order_is_configured_to_two(),
            and => A_pancake_batch_has_been_created(),
            and => An_order_request_with_two_items(),
            when => The_order_is_submitted(),
            then => The_response_should_indicate_success());
    }
}
