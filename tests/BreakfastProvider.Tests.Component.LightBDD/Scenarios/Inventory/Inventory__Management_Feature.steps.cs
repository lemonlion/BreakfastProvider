using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Inventory;
using BreakfastProvider.Tests.Component.Shared.Models.Inventory;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Inventory;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Inventory__Management_Feature : BaseFixture
{
    private readonly PostInventorySteps _postSteps;
    private readonly GetInventorySteps _getSteps;
    private readonly PutInventorySteps _putSteps;
    private readonly DeleteInventorySteps _deleteSteps;
    private int _createdItemId;

    public Inventory__Management_Feature()
    {
        _postSteps = Get<PostInventorySteps>();
        _getSteps = Get<GetInventorySteps>();
        _putSteps = Get<PutInventorySteps>();
        _deleteSteps = Get<DeleteInventorySteps>();
    }

    #region Given

    private async Task A_valid_inventory_item_request()
    {
        _postSteps.Request = new TestInventoryItemRequest
        {
            Name = $"Flour-{Guid.NewGuid():N}",
            Category = "Dry Goods",
            Quantity = 50.5m,
            Unit = "kg",
            ReorderLevel = 10m
        };
    }

    private async Task<CompositeStep> An_inventory_item_exists()
    {
        return Sub.Steps(
            _ => A_valid_inventory_item_request(),
            _ => The_inventory_item_is_submitted(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdItemId = _postSteps.Response!.Id;
    }

    #endregion

    #region When

    private async Task The_inventory_item_is_submitted()
        => await _postSteps.Send();

    private async Task The_inventory_item_is_retrieved_by_id()
        => await _getSteps.RetrieveById(_createdItemId);

    private async Task All_inventory_items_are_requested()
        => await _getSteps.RetrieveAll();

    private async Task The_inventory_item_is_updated()
    {
        _putSteps.Request = new TestInventoryItemRequest
        {
            Name = _postSteps.Response!.Name,
            Category = "Updated Category",
            Quantity = 100m,
            Unit = "kg",
            ReorderLevel = 20m
        };
        await _putSteps.Send(_createdItemId);
    }

    private async Task The_inventory_item_is_deleted()
        => await _deleteSteps.Send(_createdItemId);

    private async Task A_non_existent_inventory_item_is_retrieved()
        => await _getSteps.RetrieveById(99999);

    #endregion

    #region Then

    private async Task<CompositeStep> The_inventory_response_should_contain_the_created_item()
    {
        return Sub.Steps(
            _ => The_post_response_http_status_should_be_created(),
            _ => The_post_response_should_be_valid_json(),
            _ => The_created_item_should_have_the_correct_name(),
            _ => The_created_item_should_have_the_correct_category());
    }

    private async Task The_post_response_http_status_should_be_created()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

    private async Task The_post_response_should_be_valid_json()
        => await _postSteps.ParseResponse();

    private async Task The_created_item_should_have_the_correct_name()
        => _postSteps.Response!.Name.Should().Be(_postSteps.Request.Name);

    private async Task The_created_item_should_have_the_correct_category()
        => _postSteps.Response!.Category.Should().Be("Dry Goods");

    private async Task<CompositeStep> The_inventory_get_response_should_contain_the_item()
    {
        return Sub.Steps(
            _ => The_get_response_http_status_should_be_ok(),
            _ => The_get_response_should_be_valid_json(),
            _ => The_retrieved_item_should_match_the_created_item());
    }

    private async Task The_get_response_http_status_should_be_ok()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_get_response_should_be_valid_json()
        => await _getSteps.ParseResponse();

    private async Task The_retrieved_item_should_match_the_created_item()
    {
        _getSteps.Response!.Id.Should().Be(_createdItemId);
        _getSteps.Response!.Name.Should().Be(_postSteps.Response!.Name);
    }

    private async Task<CompositeStep> The_inventory_list_response_should_contain_the_item()
    {
        return Sub.Steps(
            _ => The_list_response_http_status_should_be_ok(),
            _ => The_list_response_should_be_valid_json(),
            _ => The_list_should_contain_the_created_item());
    }

    private async Task The_list_response_http_status_should_be_ok()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_list_response_should_be_valid_json()
        => await _getSteps.ParseListResponse();

    private async Task The_list_should_contain_the_created_item()
        => _getSteps.ListResponse!.Should().Contain(i => i.Id == _createdItemId);

    private async Task<CompositeStep> The_inventory_update_response_should_contain_the_updated_values()
    {
        return Sub.Steps(
            _ => The_put_response_http_status_should_be_ok(),
            _ => The_put_response_should_be_valid_json(),
            _ => The_updated_item_should_have_the_new_category());
    }

    private async Task The_put_response_http_status_should_be_ok()
        => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_put_response_should_be_valid_json()
        => await _putSteps.ParseResponse();

    private async Task The_updated_item_should_have_the_new_category()
        => _putSteps.Response!.Category.Should().Be("Updated Category");

    private async Task The_inventory_delete_response_should_indicate_no_content()
        => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    private async Task The_inventory_get_response_should_indicate_not_found()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    #endregion
}
