using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Order status transitions following the order lifecycle")]
public partial class Orders__Status_Transition_Feature
{
    [Scenario]
    [InlineData("Created", "Preparing")]
    [InlineData("Created", "Cancelled")]
    [InlineData("Preparing", "Ready")]
    [InlineData("Ready", "Completed")]
    public async Task A_Valid_Status_Transition_Should_Update_The_Order(string fromStatus, string toStatus)
    {
        await Runner.RunScenarioAsync(
            given => An_order_exists_with_status(fromStatus),
            when => The_order_status_is_updated_to(toStatus),
            then => The_order_status_should_be_updated_successfully(toStatus));
    }

    [Scenario]
    [InlineData("Created", "Ready")]
    [InlineData("Created", "Completed")]
    [InlineData("Preparing", "Cancelled")]
    [InlineData("Ready", "Preparing")]
    [InlineData("Completed", "Preparing")]
    [InlineData("Cancelled", "Preparing")]
    [InlineData("Cancelled", "Ready")]
    public async Task An_Invalid_Status_Transition_Should_Return_A_Conflict_Response(string fromStatus, string toStatus)
    {
        await Runner.RunScenarioAsync(
            given => An_order_exists_with_status(fromStatus),
            when => The_order_status_is_updated_to(toStatus),
            then => The_response_should_indicate_an_invalid_state_transition());
    }
}
