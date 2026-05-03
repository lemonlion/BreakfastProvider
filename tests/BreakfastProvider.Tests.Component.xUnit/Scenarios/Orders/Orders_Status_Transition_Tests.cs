using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Orders;

public class Orders_Status_Transition_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly PatchOrderStatusSteps _patchSteps;

    private Guid _orderId;

    public Orders_Status_Transition_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _patchSteps = Get<PatchOrderStatusSteps>();
    }

    private async Task CreateOrderWithStatus(string status)
    {
        // Create a pancake batch
        await _milkSteps.Retrieve();
        await _eggsSteps.Retrieve();
        await _flourSteps.Retrieve();
        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
        await _pancakeSteps.ParseResponse();

        // Create an order
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"TestCustomer_{Random.Shared.NextInt64()}",
            Items = [new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = _pancakeSteps.Response!.BatchId, Quantity = 1 }],
            TableNumber = 5
        };
        await _orderSteps.Send();
        await _orderSteps.ParseResponse();
        _orderId = _orderSteps.Response!.OrderId;

        // Walk the state machine from Created to the target status
        var path = GetTransitionPath(status);
        foreach (var intermediateStatus in path)
        {
            await _patchSteps.Send(_orderId, intermediateStatus);
            Track.That(() => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        }
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

    [Theory]
    [InlineData("Created", "Preparing")]
    [InlineData("Created", "Cancelled")]
    [InlineData("Preparing", "Ready")]
    [InlineData("Ready", "Completed")]
    public async Task Valid_status_transition_should_update_the_order(string fromStatus, string toStatus)
    {
        // Given an order exists with the given status
        await CreateOrderWithStatus(fromStatus);

        // When the order status is updated
        await _patchSteps.Send(_orderId, toStatus);

        // Then the order status should be updated successfully
        Track.That(() => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _patchSteps.ParseResponse();
        Track.That(() => _patchSteps.Response!.Status.Should().Be(toStatus));
    }

    [Theory]
    [InlineData("Created", "Ready")]
    [InlineData("Created", "Completed")]
    [InlineData("Preparing", "Cancelled")]
    [InlineData("Ready", "Preparing")]
    [InlineData("Completed", "Preparing")]
    [InlineData("Cancelled", "Preparing")]
    [InlineData("Cancelled", "Ready")]
    public async Task Invalid_status_transition_should_return_conflict(string fromStatus, string toStatus)
    {
        // Given an order exists with the given status
        await CreateOrderWithStatus(fromStatus);

        // When the order status is updated
        await _patchSteps.Send(_orderId, toStatus);

        // Then the response should indicate an invalid state transition
        Track.That(() => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict));
    }
}
