using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Inventory;

[FeatureDescription($"/{Endpoints.Inventory} - Managing ingredient inventory with full CRUD operations")]
public partial class Inventory__Management_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Adding_A_New_Inventory_Item_Should_Return_The_Created_Item()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_inventory_item_request(),
            when => The_inventory_item_is_submitted(),
            then => The_inventory_response_should_contain_the_created_item());
    }

    [Scenario]
    public async Task Retrieving_An_Existing_Inventory_Item_Should_Return_The_Item()
    {
        await Runner.RunScenarioAsync(
            given => An_inventory_item_exists(),
            when => The_inventory_item_is_retrieved_by_id(),
            then => The_inventory_get_response_should_contain_the_item());
    }

    [Scenario]
    public async Task Listing_All_Inventory_Items_Should_Return_All_Items()
    {
        await Runner.RunScenarioAsync(
            given => An_inventory_item_exists(),
            when => All_inventory_items_are_requested(),
            then => The_inventory_list_response_should_contain_the_item());
    }

    [Scenario]
    public async Task Updating_An_Inventory_Item_Should_Return_The_Updated_Item()
    {
        await Runner.RunScenarioAsync(
            given => An_inventory_item_exists(),
            when => The_inventory_item_is_updated(),
            then => The_inventory_update_response_should_contain_the_updated_values());
    }

    [Scenario]
    public async Task Deleting_An_Inventory_Item_Should_Return_No_Content()
    {
        await Runner.RunScenarioAsync(
            given => An_inventory_item_exists(),
            when => The_inventory_item_is_deleted(),
            then => The_inventory_delete_response_should_indicate_no_content());
    }

    [Scenario]
    public async Task Retrieving_A_Non_Existent_Inventory_Item_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            when => A_non_existent_inventory_item_is_retrieved(),
            then => The_inventory_get_response_should_indicate_not_found());
    }
}
