using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

[FeatureDescription($"/{Endpoints.Toppings} - Deleting toppings from the system")]
public partial class Toppings__Deletion_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Deleting_An_Existing_Topping_Should_Return_No_Content()
    {
        await Runner.RunScenarioAsync(
            given => A_known_topping_exists(),
            when => The_topping_is_deleted(),
            then => The_delete_response_should_indicate_success());
    }

    [Scenario]
    public async Task Deleting_A_Non_Existent_Topping_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            given => A_topping_id_that_does_not_exist(),
            when => The_topping_is_deleted(),
            then => The_delete_response_should_indicate_not_found());
    }
}
