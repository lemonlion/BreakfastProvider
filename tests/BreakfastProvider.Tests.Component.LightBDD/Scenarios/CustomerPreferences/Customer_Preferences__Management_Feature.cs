using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.CustomerPreferences;

[FeatureDescription($"/{Endpoints.CustomerPreferences} - Managing customer breakfast preferences")]
public partial class Customer_Preferences__Management_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Saving_Customer_Preferences_Should_Return_The_Saved_Preferences()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_customer_preference_request(),
            when => The_customer_preferences_are_saved(),
            then => The_preference_response_should_contain_the_saved_preferences());
    }

    [Scenario]
    public async Task Retrieving_Existing_Customer_Preferences_Should_Return_The_Preferences()
    {
        await Runner.RunScenarioAsync(
            given => Customer_preferences_exist(),
            when => The_customer_preferences_are_retrieved(),
            then => The_preference_get_response_should_contain_the_preferences());
    }

    [Scenario]
    public async Task Updating_Customer_Preferences_Should_Return_The_Updated_Preferences()
    {
        await Runner.RunScenarioAsync(
            given => Customer_preferences_exist(),
            when => The_customer_preferences_are_updated(),
            then => The_preference_update_response_should_contain_the_updated_values());
    }

    [Scenario]
    public async Task Retrieving_Non_Existent_Customer_Preferences_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            when => Non_existent_customer_preferences_are_retrieved(),
            then => The_preference_get_response_should_indicate_not_found());
    }

    [Scenario]
    public async Task Saving_Customer_Preferences_With_Missing_Customer_Name_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_customer_preference_request_with_missing_customer_name(),
            when => The_customer_preferences_are_saved(),
            then => The_preference_response_should_indicate_bad_request());
    }
}
