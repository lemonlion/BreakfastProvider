using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

[FeatureDescription($"/{Endpoints.Toppings} - Listing available toppings and adding custom toppings")]
public partial class Toppings__Management_Feature
{
    [HappyPath]
    [Scenario]
    public async Task The_Toppings_Endpoint_Should_Return_All_Available_Toppings()
    {
        await Runner.RunScenarioAsync(
            when => The_available_toppings_are_requested(),
            then => The_toppings_response_should_contain_the_default_toppings());
    }

    [Scenario]
    public async Task Adding_A_New_Topping_Should_Return_The_Created_Topping()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_topping_request(),
            when => The_new_topping_is_submitted(),
            then => The_topping_response_should_contain_the_created_topping());
    }
}
