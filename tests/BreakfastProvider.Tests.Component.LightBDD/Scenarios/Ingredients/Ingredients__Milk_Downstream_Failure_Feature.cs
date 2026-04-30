using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Ingredients;

[FeatureDescription($"/{Endpoints.Milk} - Handling downstream Cow Service failures")]
public partial class Ingredients__Milk_Downstream_Failure_Feature
{
    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Requesting_Milk_When_The_Cow_Service_Is_Unavailable_Should_Return_A_Bad_Gateway_Response()
    {
        await Runner.RunScenarioAsync(
            given => The_cow_service_will_return_service_unavailable(),
            when => Milk_is_requested(),
            then => The_milk_response_should_indicate_a_bad_gateway());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Requesting_Milk_When_The_Cow_Service_Times_Out_Should_Return_A_Bad_Gateway_Response()
    {
        await Runner.RunScenarioAsync(
            given => The_cow_service_will_return_a_timeout(),
            when => Milk_is_requested(),
            then => The_milk_response_should_indicate_a_bad_gateway());
    }

    [Scenario]
    [IgnoreIf(nameof(Settings.RunAgainstExternalServiceUnderTest), NeedsToControlFakeResponses)]
    public async Task Requesting_Milk_When_The_Cow_Service_Returns_An_Invalid_Response_Should_Return_A_Bad_Gateway_Response()
    {
        await Runner.RunScenarioAsync(
            given => The_cow_service_will_return_an_invalid_response(),
            when => Milk_is_requested(),
            then => The_milk_response_should_indicate_a_bad_gateway());
    }
}
