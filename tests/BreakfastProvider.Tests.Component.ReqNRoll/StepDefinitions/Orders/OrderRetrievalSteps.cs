using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class OrderRetrievalSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps,
    GetOrderSteps getOrderSteps)
{
    private Guid _orderId;

    [Given("an order has been created")]
    public async Task GivenAnOrderHasBeenCreated()
    {
        await milkSteps.Retrieve();
        await eggsSteps.Retrieve();
        await flourSteps.Retrieve();

        pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = milkSteps.MilkResponse.Milk,
            Eggs = eggsSteps.EggsResponse.Eggs,
            Flour = flourSteps.FlourResponse.Flour
        };
        await pancakeSteps.Send();
        await pancakeSteps.ParseResponse();

        orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"RetrievalTestCustomer_{Random.Shared.NextInt64()}",
            Items = [new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = pancakeSteps.Response!.BatchId, Quantity = 1 }],
            TableNumber = 3
        };
        await orderSteps.Send();
        await orderSteps.ParseResponse();
        _orderId = orderSteps.Response!.OrderId;
    }

    [When("the order is retrieved by id")]
    public async Task WhenTheOrderIsRetrievedById()
    {
        await getOrderSteps.Retrieve(_orderId);
    }

    [When("a non-existent order is retrieved")]
    public async Task WhenANonExistentOrderIsRetrieved()
    {
        await getOrderSteps.Retrieve(Guid.NewGuid());
    }

    [Then("the order retrieval response should contain the order")]
    public async Task ThenTheOrderRetrievalResponseShouldContainTheOrder()
    {
        getOrderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getOrderSteps.ParseResponse();
        getOrderSteps.Response!.OrderId.Should().Be(_orderId);
    }

    [Then("the order retrieval response should indicate not found")]
    public void ThenTheOrderRetrievalResponseShouldIndicateNotFound()
    {
        getOrderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
