using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998
public partial class Orders__Status_Transition_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly PatchOrderStatusSteps _patchSteps;

    private Guid _orderId;

    public Orders__Status_Transition_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _patchSteps = Get<PatchOrderStatusSteps>();
    }

    #region Given

    private async Task<CompositeStep> An_order_exists_with_status(string status)
    {
        return Sub.Steps(
            _ => A_pancake_batch_is_created(),
            _ => An_order_is_created_for_the_batch(),
            _ => The_order_is_transitioned_to_status(status));
    }

    private async Task A_pancake_batch_is_created()
    {
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
    }

    private async Task An_order_is_created_for_the_batch()
    {
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"TestCustomer_{Random.Shared.NextInt64()}",
            Items = [new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = _pancakeSteps.Response!.BatchId, Quantity = 1 }],
            TableNumber = 5
        };
        await _orderSteps.Send();
        await _orderSteps.ParseResponse();
        _orderId = _orderSteps.Response!.OrderId;
    }

    private async Task The_order_is_transitioned_to_status(string targetStatus)
    {
        // Walk the state machine from Created to the target status
        var path = GetTransitionPath(targetStatus);
        foreach (var intermediateStatus in path)
        {
            await _patchSteps.Send(_orderId, intermediateStatus);
            _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
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

    #endregion

    #region When

    private async Task The_order_status_is_updated_to(string toStatus)
        => await _patchSteps.Send(_orderId, toStatus);

    #endregion

    #region Then

    private async Task<CompositeStep> The_order_status_should_be_updated_successfully(string expectedStatus)
    {
        return Sub.Steps(
            _ => The_patch_response_http_status_should_be_ok(),
            _ => The_updated_order_status_should_be(expectedStatus));
    }

    private async Task The_patch_response_http_status_should_be_ok()
        => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_updated_order_status_should_be(string expectedStatus)
    {
        await _patchSteps.ParseResponse();
        _patchSteps.Response!.Status.Should().Be(expectedStatus);
    }

    private async Task The_response_should_indicate_an_invalid_state_transition()
        => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict);

    #endregion
}
