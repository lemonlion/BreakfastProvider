using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Updating the status of a non-existent order")]
public partial class Orders__Status_Update_Not_Found_Feature
{
    [Scenario]
    public async Task Updating_The_Status_Of_A_Non_Existent_Order_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            given => A_non_existent_order_id(),
            when => The_order_status_is_updated_to_preparing(),
            then => The_response_should_indicate_not_found());
    }
}
