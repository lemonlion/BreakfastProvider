using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

[FeatureDescription($"/{Endpoints.Orders} - Structured logging and telemetry verification")]
[IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), IgnoreReasons.NeedsNonDefaultConfiguration)]
public partial class Infrastructure__Telemetry_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Creating_An_Order_Should_Emit_A_Structured_Log_Entry()
    {
        await Runner.RunScenarioAsync(
            given => The_application_is_configured_with_an_in_memory_log_capture(),
            and => A_pancake_batch_has_been_created(),
            and => A_valid_order_request(),
            when => The_order_is_submitted(),
            then => A_structured_log_entry_should_have_been_captured_for_order_creation());
    }
}
