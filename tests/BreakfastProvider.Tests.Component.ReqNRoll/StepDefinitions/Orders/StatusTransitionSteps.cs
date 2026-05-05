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
public class StatusTransitionSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps,
    PatchOrderStatusSteps patchSteps)
{
    private Guid _orderId;

    [Given(@"an order exists with status ""(.*)""")]
    public async Task GivenAnOrderExistsWithStatus(string status)
    {
        // Create pancake batch
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

        // Create order
        orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"TestCustomer_{Random.Shared.NextInt64()}",
            Items = [new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = pancakeSteps.Response!.BatchId, Quantity = 1 }],
            TableNumber = 5
        };
        await orderSteps.Send();
        await orderSteps.ParseResponse();
        _orderId = orderSteps.Response!.OrderId;

        // Transition to target status
        var path = GetTransitionPath(status);
        foreach (var intermediateStatus in path)
        {
            await patchSteps.Send(_orderId, intermediateStatus);
            patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [When(@"the order status is updated to ""(.*)""")]
    public async Task WhenTheOrderStatusIsUpdatedTo(string toStatus)
    {
        await patchSteps.Send(_orderId, toStatus);
    }

    [Then(@"the order status should be updated successfully to ""(.*)""")]
    public async Task ThenTheOrderStatusShouldBeUpdatedSuccessfullyTo(string expectedStatus)
    {
        patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await patchSteps.ParseResponse();
        patchSteps.Response!.Status.Should().Be(expectedStatus);
    }

    [Then("the response should indicate an invalid state transition")]
    public void ThenTheResponseShouldIndicateAnInvalidStateTransition()
    {
        patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static List<string> GetTransitionPath(string targetStatus) => targetStatus switch
    {
        OrderStatuses.Created => [],
        OrderStatuses.Preparing => [OrderStatuses.Preparing],
        OrderStatuses.Ready => [OrderStatuses.Preparing, OrderStatuses.Ready],
        OrderStatuses.Completed => [OrderStatuses.Preparing, OrderStatuses.Ready, OrderStatuses.Completed],
        OrderStatuses.Cancelled => [OrderStatuses.Cancelled],
        _ => []
    };
}
