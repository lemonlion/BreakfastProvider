using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Grpc;

[FeatureDescription("/grpc - Streaming order updates via gRPC server streaming")]
public partial class Grpc__Stream_Order_Updates_Feature
{
    #region Happy Path

    [HappyPath]
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsInProcessGrpc)]
    public async Task Streaming_Order_Updates_Should_Return_The_Current_Status()
    {
        await Runner.RunScenarioAsync(
            given => A_pancake_batch_has_been_created(),
            and => An_order_has_been_created_for_the_batch(),
            when => Order_updates_are_streamed_via_grpc(),
            then => The_streamed_response_should_contain_the_order_status());
    }

    #endregion

    #region Not Found

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsInProcessGrpc)]
    public async Task Streaming_Updates_For_Non_Existent_Order_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            when => Order_updates_for_a_non_existent_order_are_streamed_via_grpc(),
            then => The_grpc_stream_should_return_a_not_found_error());
    }

    #endregion
}
