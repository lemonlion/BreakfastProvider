using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Inventory;
using BreakfastProvider.Tests.Component.Shared.Models.Inventory;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Inventory;

[Binding]
public class InventoryManagementSteps(
    PostInventorySteps postSteps,
    GetInventorySteps getSteps,
    PutInventorySteps putSteps,
    DeleteInventorySteps deleteSteps)
{
    private int _createdItemId;

    [Given("a valid inventory item request")]
    public void GivenAValidInventoryItemRequest()
    {
        postSteps.Request = new TestInventoryItemRequest
        {
            Name = $"Flour-{Guid.NewGuid():N}",
            Category = "Dry Goods",
            Quantity = 50.5m,
            Unit = "kg",
            ReorderLevel = 10m
        };
    }

    [Given("an inventory item exists")]
    public async Task GivenAnInventoryItemExists()
    {
        GivenAValidInventoryItemRequest();
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _createdItemId = postSteps.Response!.Id;
    }

    [When("the inventory item is submitted")]
    public async Task WhenTheInventoryItemIsSubmitted() => await postSteps.Send();

    [When("the inventory item is retrieved by id")]
    public async Task WhenTheInventoryItemIsRetrievedById() => await getSteps.RetrieveById(_createdItemId);

    [When("all inventory items are requested")]
    public async Task WhenAllInventoryItemsAreRequested() => await getSteps.RetrieveAll();

    [When("the inventory item is updated")]
    public async Task WhenTheInventoryItemIsUpdated()
    {
        putSteps.Request = new TestInventoryItemRequest
        {
            Name = postSteps.Response!.Name,
            Category = "Updated Category",
            Quantity = 100m,
            Unit = "kg",
            ReorderLevel = 20m
        };
        await putSteps.Send(_createdItemId);
    }

    [When("the inventory item is deleted")]
    public async Task WhenTheInventoryItemIsDeleted() => await deleteSteps.Send(_createdItemId);

    [When("a non-existent inventory item is retrieved")]
    public async Task WhenANonExistentInventoryItemIsRetrieved() => await getSteps.RetrieveById(99999);

    [Then("the inventory response should contain the created item")]
    public async Task ThenTheInventoryResponseShouldContainTheCreatedItem()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.Name.Should().Be(postSteps.Request.Name);
        postSteps.Response!.Category.Should().Be("Dry Goods");
    }

    [Then("the inventory get response should contain the item")]
    public async Task ThenTheInventoryGetResponseShouldContainTheItem()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseResponse();
        getSteps.Response!.Id.Should().Be(_createdItemId);
        getSteps.Response!.Name.Should().Be(postSteps.Response!.Name);
    }

    [Then("the inventory list response should contain the item")]
    public async Task ThenTheInventoryListResponseShouldContainTheItem()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseListResponse();
        getSteps.ListResponse!.Should().Contain(i => i.Id == _createdItemId);
    }

    [Then("the inventory update response should contain the updated values")]
    public async Task ThenTheInventoryUpdateResponseShouldContainTheUpdatedValues()
    {
        putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await putSteps.ParseResponse();
        putSteps.Response!.Category.Should().Be("Updated Category");
    }

    [Then("the inventory delete response should indicate no content")]
    public void ThenTheInventoryDeleteResponseShouldIndicateNoContent()
        => deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    [Then("the inventory get response should indicate not found")]
    public void ThenTheInventoryGetResponseShouldIndicateNotFound()
        => getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
