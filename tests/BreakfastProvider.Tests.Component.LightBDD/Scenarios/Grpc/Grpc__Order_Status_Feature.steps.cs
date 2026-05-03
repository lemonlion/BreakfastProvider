using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Grpc.Core;
using LightBDD.Framework;
using TestTrackingDiagrams.LightBDD;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Grpc;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Grpc__Order_Status_Feature : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GrpcBreakfastSteps _grpcSteps;

    public Grpc__Order_Status_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _grpcSteps = Get<GrpcBreakfastSteps>();
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _grpcSteps.Initialize(AppFactory, CurrentTestInfo.Fetcher);
    }

    private Guid _createdOrderId;

    #region Given

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => A_pancake_request_is_submitted_with_ingredients(),
            _ => The_pancake_batch_response_should_be_successful());
    }

    private async Task A_pancake_request_is_submitted_with_ingredients()
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
    }

    private async Task The_pancake_batch_response_should_be_successful()
    {
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
    }

    private async Task<CompositeStep> An_order_has_been_created_for_the_batch()
    {
        return Sub.Steps(
            _ => An_order_request_is_submitted(),
            _ => The_order_creation_response_should_be_successful());
    }

    private async Task An_order_request_is_submitted()
    {
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = _customerName,
            TableNumber = 5,
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = _pancakeSteps.Response!.BatchId,
                    Quantity = 1
                }
            ]
        };
        await _orderSteps.Send();
    }

    private async Task The_order_creation_response_should_be_successful()
    {
        Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _orderSteps.ParseResponse();
        _createdOrderId = _orderSteps.Response!.OrderId;
    }

    #endregion

    #region When

    private async Task The_order_status_is_requested_via_grpc()
        => await _grpcSteps.GetOrderStatus(_createdOrderId.ToString());

    private async Task The_order_status_for_a_non_existent_order_is_requested_via_grpc()
        => await _grpcSteps.GetOrderStatus(Guid.NewGuid().ToString());

    #endregion

    #region Then

    private async Task<CompositeStep> The_grpc_response_should_contain_the_order_details()
    {
        return Sub.Steps(
            _ => The_grpc_order_id_should_match(),
            _ => The_grpc_customer_name_should_match(),
            _ => The_grpc_status_should_be_created());
    }

    private async Task The_grpc_order_id_should_match()
        => Track.That(() => _grpcSteps.OrderStatusReply!.OrderId.Should().Be(_createdOrderId.ToString()));

    private async Task The_grpc_customer_name_should_match()
        => Track.That(() => _grpcSteps.OrderStatusReply!.CustomerName.Should().Be(_customerName));

    private async Task The_grpc_status_should_be_created()
        => Track.That(() => _grpcSteps.OrderStatusReply!.Status.Should().Be(OrderStatuses.Created));

    private async Task The_grpc_response_should_be_a_not_found_error()
    {
        Track.That(() => _grpcSteps.RpcException.Should().NotBeNull());
        Track.That(() => _grpcSteps.RpcException!.StatusCode.Should().Be(StatusCode.NotFound));
    }

    #endregion
}
