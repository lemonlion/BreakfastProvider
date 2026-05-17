using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.IngredientWaste;

[FeatureDescription($"/{Endpoints.IngredientWaste} - Recording and managing ingredient waste (BigQuery)")]
public partial class Ingredient_Waste__Tracking_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Recording_Ingredient_Waste_Should_Return_The_Created_Record()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_ingredient_waste_request(),
            when => The_waste_is_recorded(),
            then => The_response_should_contain_the_created_waste_record());
    }

    [Scenario]
    public async Task Listing_Waste_By_Recipe_Should_Return_Matching_Records()
    {
        await Runner.RunScenarioAsync(
            given => An_ingredient_waste_record_exists(),
            when => The_waste_is_listed_by_recipe(),
            then => The_list_response_should_contain_the_waste_record());
    }

    [Scenario]
    public async Task Deleting_A_Waste_Record_Should_Return_No_Content()
    {
        await Runner.RunScenarioAsync(
            given => An_ingredient_waste_record_exists(),
            when => The_waste_record_is_deleted(),
            then => The_delete_response_should_indicate_no_content());
    }

    [Scenario]
    public async Task Recording_Waste_With_Missing_Ingredient_Name_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_waste_request_with_missing_ingredient_name(),
            when => The_waste_is_recorded(),
            then => The_waste_response_should_indicate_bad_request());
    }

    [Scenario]
    public async Task Recording_Waste_With_Zero_Quantity_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_waste_request_with_zero_quantity(),
            when => The_waste_is_recorded(),
            then => The_waste_response_should_indicate_bad_request());
    }

    [Scenario]
    public async Task Recording_Waste_With_Missing_Reason_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_waste_request_with_missing_reason(),
            when => The_waste_is_recorded(),
            then => The_waste_response_should_indicate_bad_request());
    }
}
