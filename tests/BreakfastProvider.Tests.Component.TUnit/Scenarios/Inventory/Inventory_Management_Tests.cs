using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Inventory;
using BreakfastProvider.Tests.Component.Shared.Models.Inventory;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Inventory;

public class Inventory_Management_Tests : BaseFixture
{
    private readonly PostInventorySteps _postSteps;
    private readonly GetInventorySteps _getSteps;
    private readonly PutInventorySteps _putSteps;
    private readonly DeleteInventorySteps _deleteSteps;

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

    private async Task<int> CreateInventoryItem()
    {
        _postSteps.Request = CreateValidRequest();
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        return _postSteps.Response!.Id;
    }

    [Test]
    [HappyPath]
    public async Task Adding_a_new_inventory_item_should_return_the_created_item()
    {
        // Given a valid inventory item request
        _postSteps.Request = CreateValidRequest();

        // When the inventory item is submitted
        await _postSteps.Send();

        // Then the response should contain the created item
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.Name.Should().Be(_postSteps.Request.Name);
        _postSteps.Response!.Category.Should().Be("Dry Goods");
    }

    [Test]
    public async Task Retrieving_an_existing_inventory_item_should_return_the_item()
    {
        // Given an inventory item exists
        var createdItemId = await CreateInventoryItem();

        // When the inventory item is retrieved by id
        await _getSteps.RetrieveById(createdItemId);

        // Then the response should contain the item
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.Id.Should().Be(createdItemId);
        _getSteps.Response!.Name.Should().Be(_postSteps.Response!.Name);
    }

    [Test]
    public async Task Listing_all_inventory_items_should_return_all_items()
    {
        // Given an inventory item exists
        var createdItemId = await CreateInventoryItem();

        // When all inventory items are requested
        await _getSteps.RetrieveAll();

        // Then the list response should contain the item
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(i => i.Id == createdItemId);
    }

    [Test]
    public async Task Updating_an_inventory_item_should_return_the_updated_item()
    {
        // Given an inventory item exists
        var createdItemId = await CreateInventoryItem();

        // When the inventory item is updated
        _putSteps.Request = new TestInventoryItemRequest
        {
            Name = _postSteps.Response!.Name,
            Category = "Updated Category",
            Quantity = 100m,
            Unit = "kg",
            ReorderLevel = 20m
        };
        await _putSteps.Send(createdItemId);

        // Then the response should contain the updated values
        _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _putSteps.ParseResponse();
        _putSteps.Response!.Category.Should().Be("Updated Category");
    }

    [Test]
    public async Task Deleting_an_inventory_item_should_return_no_content()
    {
        // Given an inventory item exists
        var createdItemId = await CreateInventoryItem();

        // When the inventory item is deleted
        await _deleteSteps.Send(createdItemId);

        // Then the response should indicate no content
        _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Retrieving_a_non_existent_inventory_item_should_return_not_found()
    {
        // When a non-existent inventory item is retrieved
        await _getSteps.RetrieveById(99999);

        // Then the response should indicate not found
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
