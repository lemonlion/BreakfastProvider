using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Creating orders when the Kitchen Service returns an error")]
public partial class Orders__Kitchen_Service_Failure_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Creating_An_Order_When_The_Kitchen_Service_Returns_An_Error_Should_Still_Create_The_Order()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => A_valid_order_request_for_the_created_batch(),
            and => The_kitchen_service_will_return_an_error(),
            when => The_breakfast_order_is_placed(),
            then => The_order_should_still_be_created_successfully(),
            and => The_order_should_be_retrievable_by_its_id());
    }
}
