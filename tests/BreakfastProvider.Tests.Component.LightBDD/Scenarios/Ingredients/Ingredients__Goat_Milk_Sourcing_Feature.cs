using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Ingredients;

[FeatureDescription($"/{Endpoints.GoatMilk} - Sourcing goat milk from the Goat Service")]
public partial class Ingredients__Goat_Milk_Sourcing_Feature
{
    [HappyPath]
    [Scenario]
    public async Task The_Goat_Milk_Endpoint_Should_Return_Fresh_Goat_Milk_From_The_Goat_Service()
    {
        await Runner.RunScenarioAsync(
            when => Goat_milk_is_requested(),
            then => The_goat_milk_response_should_contain_fresh_goat_milk(),
            and => The_goat_service_should_have_received_a_goat_milk_request());
    }
}
