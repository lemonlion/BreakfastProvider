using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Inventory;
using BreakfastProvider.Tests.Component.Shared.Models.Inventory;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Inventory;

public class Inventory_Management_Tests : BaseFixture
{
    private readonly PostInventorySteps _postSteps;
    private readonly GetInventorySteps _getSteps;
    private readonly PutInventorySteps _putSteps;
    private readonly DeleteInventorySteps _deleteSteps;

    private int _createdItemId;

    public Inventory_Management_Tests()
    {
        _postSteps = Get<PostInventorySteps>();
        _getSteps = Get<GetInventorySteps>();
        _putSteps = Get<PutInventorySteps>();
        _deleteSteps = Get<DeleteInventorySteps>();
    }

    private TestInventoryItemRequest CreateValidRequest() => new()
    {
        Name = $"Flour-{Guid.NewGuid():N}",
        Category = "Dry Goods",
        Quantity = 50.5m,
        Unit = "kg",
        ReorderLevel = 10m
    };

    [Fact]
    [HappyPath]
    public void Adding_a_new_inventory_item_should_return_the_created_item()
    {
        this.Given(x => x.A_valid_inventory_item_request_is_prepared())
            .When(x => x.The_inventory_item_is_submitted())
            .Then(x => x.The_response_should_contain_the_created_item())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_an_existing_inventory_item_should_return_the_item()
    {
        this.Given(x => x.An_inventory_item_exists())
            .When(x => x.The_inventory_item_is_retrieved_by_id())
            .Then(x => x.The_get_response_should_contain_the_item())
            .BDDfy();
    }

    [Fact]
    public void Listing_all_inventory_items_should_return_all_items()
    {
        this.Given(x => x.An_inventory_item_exists())
            .When(x => x.All_inventory_items_are_requested())
            .Then(x => x.The_list_response_should_contain_the_item())
            .BDDfy();
    }

    [Fact]
    public void Updating_an_inventory_item_should_return_the_updated_item()
    {
        this.Given(x => x.An_inventory_item_exists())
            .When(x => x.The_inventory_item_is_updated())
            .Then(x => x.The_put_response_should_contain_the_updated_values())
            .BDDfy();
    }

    [Fact]
    public void Deleting_an_inventory_item_should_return_no_content()
    {
        this.Given(x => x.An_inventory_item_exists())
            .When(x => x.The_inventory_item_is_deleted())
            .Then(x => x.The_delete_response_should_indicate_no_content())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_a_non_existent_inventory_item_should_return_not_found()
    {
        this.When(x => x.A_non_existent_inventory_item_is_retrieved())
            .Then(x => x.The_get_response_should_indicate_not_found())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_inventory_item_request_is_prepared()
    {
        _postSteps.Request = CreateValidRequest();
        await Task.CompletedTask;
    }

    private async Task The_inventory_item_is_submitted()
    {
        await _postSteps.Send();
    }

    private async Task The_response_should_contain_the_created_item()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.Name.Should().Be(_postSteps.Request!.Name);
        _postSteps.Response!.Category.Should().Be("Dry Goods");
    }

    private async Task An_inventory_item_exists()
    {
        _postSteps.Request = CreateValidRequest();
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdItemId = _postSteps.Response!.Id;
    }

    private async Task The_inventory_item_is_retrieved_by_id()
    {
        await _getSteps.RetrieveById(_createdItemId);
    }

    private async Task The_get_response_should_contain_the_item()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.Id.Should().Be(_createdItemId);
        _getSteps.Response!.Name.Should().Be(_postSteps.Response!.Name);
    }

    private async Task All_inventory_items_are_requested()
    {
        await _getSteps.RetrieveAll();
    }

    private async Task The_list_response_should_contain_the_item()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(i => i.Id == _createdItemId);
    }

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

    private async Task The_put_response_should_contain_the_updated_values()
    {
        _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _putSteps.ParseResponse();
        _putSteps.Response!.Category.Should().Be("Updated Category");
    }

    private async Task The_inventory_item_is_deleted()
    {
        await _deleteSteps.Send(_createdItemId);
    }

    private void The_delete_response_should_indicate_no_content()
    {
        _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task A_non_existent_inventory_item_is_retrieved()
    {
        await _getSteps.RetrieveById(99999);
    }

    private void The_get_response_should_indicate_not_found()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
