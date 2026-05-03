using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class CompleteLifecycleSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps,
    GetOrderSteps getOrderSteps,
    PatchOrderStatusSteps patchSteps,
    GetAuditLogsSteps auditSteps,
    DownstreamRequestSteps downstreamSteps)
{
    private readonly string _customerName = $"LifecycleTestCustomer_{Random.Shared.NextInt64()}";
    private Guid _orderId;

    [Given("a breakfast order has been placed for the batch")]
    public async Task GivenABreakfastOrderHasBeenPlacedForTheBatch()
    {
        orderSteps.Request = new TestOrderRequest
        {
            CustomerName = _customerName,
            TableNumber = 4,
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = pancakeSteps.Response!.BatchId,
                    Quantity = 2
                }
            ]
        };
        await orderSteps.Send();
        Track.That(() => orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await orderSteps.ParseResponse();
        _orderId = orderSteps.Response!.OrderId;
    }

    [When("the order progresses through all statuses to completed")]
    public async Task WhenTheOrderProgressesThroughAllStatusesToCompleted()
    {
        foreach (var status in new[] { OrderStatuses.Preparing, OrderStatuses.Ready, OrderStatuses.Completed })
        {
            await patchSteps.Send(_orderId, status);
            Track.That(() => patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        }
    }

    [Then("the completed order should be retrievable with all details")]
    public async Task ThenTheCompletedOrderShouldBeRetrievableWithAllDetails()
    {
        await getOrderSteps.Retrieve(_orderId);
        Track.That(() => getOrderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await getOrderSteps.ParseResponse();
        Track.That(() => getOrderSteps.Response!.OrderId.Should().Be(_orderId));
        Track.That(() => getOrderSteps.Response!.Status.Should().Be(OrderStatuses.Completed));
        Track.That(() => getOrderSteps.Response!.CustomerName.Should().Be(_customerName));
    }

    [Then("an audit log entry should exist for the order")]
    public async Task ThenAnAuditLogEntryShouldExistForTheOrder()
    {
        await auditSteps.Retrieve();
        Track.That(() => auditSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await auditSteps.ParseResponse();
        Track.That(() => auditSteps.Response!.Should().Contain(a =>
            a.Action == AuditLogDefaults.CreatedAction
            && a.EntityType == AuditLogDefaults.OrderEntityType
            && a.Details.Contains(_customerName)));
    }
}
