using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

[FeatureDescription($"/{Endpoints.Orders} - Retrieving breakfast orders by ID")]
public partial class Orders__Order_Retrieval_Feature
{
    [HappyPath]
    [Scenario]
    public async Task A_Previously_Created_Order_Should_Be_Retrievable_By_Id()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => An_order_has_been_created_for_the_batch(),
            when => The_order_is_retrieved_by_id(),
            then => The_retrieved_order_should_match_the_created_order(),
            and => The_cow_service_should_have_received_a_milk_request(),
            and => The_kitchen_service_should_have_received_a_preparation_request());
    }

    [Scenario]
    public async Task Retrieving_A_Non_Existent_Order_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            given => A_non_existent_order_id(),
            when => The_order_is_retrieved_by_id(),
            then => The_response_should_be_not_found());
    }
}
