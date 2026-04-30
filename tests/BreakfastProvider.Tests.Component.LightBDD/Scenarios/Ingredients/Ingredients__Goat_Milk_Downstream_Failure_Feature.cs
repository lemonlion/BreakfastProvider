using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Ingredients;

[FeatureDescription($"/{Endpoints.GoatMilk} - Handling downstream Goat Service failures")]
public partial class Ingredients__Goat_Milk_Downstream_Failure_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Requesting_Goat_Milk_When_The_Goat_Service_Is_Unavailable_Should_Return_A_Bad_Gateway_Response()
    {
        await Runner.RunScenarioAsync(
            given => The_goat_service_will_return_service_unavailable(),
            when => Goat_milk_is_requested(),
            then => The_goat_milk_response_should_indicate_a_bad_gateway());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Requesting_Goat_Milk_When_The_Goat_Service_Returns_An_Invalid_Response_Should_Return_A_Bad_Gateway_Response()
    {
        await Runner.RunScenarioAsync(
            given => The_goat_service_will_return_an_invalid_response(),
            when => Goat_milk_is_requested(),
            then => The_goat_milk_response_should_indicate_a_bad_gateway());
    }
}
