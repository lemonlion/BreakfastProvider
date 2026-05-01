using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Grpc;

[FeatureDescription("/grpc - Retrieving order status via gRPC")]
public partial class Grpc__Order_Status_Feature
{
    #region Happy Path

    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsInProcessGrpc)]
    public async Task Order_Status_Via_Grpc_Should_Return_Order_Details()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => An_order_has_been_created_for_the_batch(),
            when => The_order_status_is_requested_via_grpc(),
            then => The_grpc_response_should_contain_the_order_details());
    }

    #endregion

    #region Not Found

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsInProcessGrpc)]
    public async Task Order_Status_For_Non_Existent_Order_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            when => The_order_status_for_a_non_existent_order_is_requested_via_grpc(),
            then => The_grpc_response_should_be_a_not_found_error());
    }

    #endregion
}
