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
using BreakfastProvider.Tests.Component.LightBDD.Util;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Grpc;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Grpc__Stream_Order_Updates_Feature : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GrpcBreakfastSteps _grpcSteps;

    public Grpc__Stream_Order_Updates_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _grpcSteps = Get<GrpcBreakfastSteps>();
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
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
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
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        _createdOrderId = _orderSteps.Response!.OrderId;
    }

    #endregion

    #region When

    private async Task Order_updates_are_streamed_via_grpc()
        => await _grpcSteps.StreamOrderUpdates(_createdOrderId.ToString());

    private async Task Order_updates_for_a_non_existent_order_are_streamed_via_grpc()
        => await _grpcSteps.StreamOrderUpdates(Guid.NewGuid().ToString());

    #endregion

    #region Then

    private async Task<CompositeStep> The_streamed_response_should_contain_the_order_status()
    {
        return Sub.Steps(
            _ => The_stream_should_contain_one_reply(),
            _ => The_streamed_order_id_should_match(),
            _ => The_streamed_customer_name_should_match(),
            _ => The_streamed_status_should_be_created());
    }

    private async Task The_stream_should_contain_one_reply()
        => _grpcSteps.StreamedReplies.Should().HaveCount(1);

    private async Task The_streamed_order_id_should_match()
        => _grpcSteps.StreamedReplies[0].OrderId.Should().Be(_createdOrderId.ToString());

    private async Task The_streamed_customer_name_should_match()
        => _grpcSteps.StreamedReplies[0].CustomerName.Should().Be(_customerName);

    private async Task The_streamed_status_should_be_created()
        => _grpcSteps.StreamedReplies[0].Status.Should().Be(OrderStatuses.Created);

    private async Task The_grpc_stream_should_return_a_not_found_error()
    {
        _grpcSteps.RpcException.Should().NotBeNull();
        _grpcSteps.RpcException!.StatusCode.Should().Be(StatusCode.NotFound);
    }

    #endregion
}
