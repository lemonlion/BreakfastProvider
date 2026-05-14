using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.ChefNotes;

[FeatureDescription($"/{Endpoints.ChefNotes} - Creating, retrieving, and updating chef notes (MongoDB)")]
public partial class Chef_Notes__Management_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Creating_A_Chef_Note_Should_Return_The_Created_Note()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_chef_note_request(),
            when => The_note_is_submitted(),
            then => The_response_should_contain_the_created_note());
    }

    [Scenario]
    public async Task Retrieving_An_Existing_Note_By_Id_Should_Return_The_Note()
    {
        await Runner.RunScenarioAsync(
            given => A_chef_note_exists(),
            when => The_note_is_retrieved_by_id(),
            then => The_get_response_should_contain_the_note());
    }

    [Scenario]
    public async Task Updating_An_Existing_Note_Should_Return_The_Updated_Note()
    {
        await Runner.RunScenarioAsync(
            given => A_chef_note_exists(),
            when => The_note_is_updated(),
            then => The_update_response_should_contain_the_modified_note());
    }

    [Scenario]
    public async Task Listing_Notes_By_Recipe_Should_Return_Matching_Notes()
    {
        await Runner.RunScenarioAsync(
            given => A_chef_note_exists(),
            when => The_notes_are_listed_by_recipe(),
            then => The_list_response_should_contain_the_note());
    }

    [Scenario]
    public async Task Retrieving_A_Non_Existent_Note_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            when => A_non_existent_note_is_retrieved(),
            then => The_get_response_should_indicate_not_found());
    }

    [Scenario]
    public async Task Updating_A_Non_Existent_Note_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            when => A_non_existent_note_is_updated(),
            then => The_update_response_should_indicate_not_found());
    }

    [Scenario]
    public async Task Creating_A_Note_With_Missing_Recipe_Name_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_note_request_with_missing_recipe_name(),
            when => The_note_is_submitted(),
            then => The_note_response_should_indicate_bad_request());
    }

    [Scenario]
    public async Task Creating_A_Note_With_Missing_Note_Text_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_note_request_with_missing_note_text(),
            when => The_note_is_submitted(),
            then => The_note_response_should_indicate_bad_request());
    }
}
