using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Grpc.Core;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Grpc;

public class Grpc_Order_Status_Tests : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GrpcBreakfastSteps _grpcSteps;

    private string _createdOrderId = null!;

    public Grpc_Order_Status_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _grpcSteps = Get<GrpcBreakfastSteps>();
        if (Settings.RunAgainstExternalServiceUnderTest)
            _grpcSteps.InitializeExternal(Settings.ExternalGrpcUrl ?? Settings.ExternalServiceUnderTestUrl!);
        else
            _grpcSteps.Initialize(AppFactory, CurrentTestInfo.Fetcher);
    }

    [Fact]
    [HappyPath]
    public void Order_status_via_grpc_should_return_order_details()
    {
        this.Given(x => x.A_pancake_batch_and_order_have_been_created())
            .When(x => x.The_order_status_is_requested_via_grpc())
            .Then(x => x.The_grpc_response_should_contain_the_order_details())
            .BDDfy();
    }

    [Fact]
    public void Order_status_for_non_existent_order_should_return_not_found()
    {
        this.When(x => x.The_order_status_for_a_non_existent_order_is_requested_via_grpc())
            .Then(x => x.The_grpc_response_should_be_a_not_found_error())
            .BDDfy();
    }

    #region Steps

    private async Task A_pancake_batch_and_order_have_been_created()
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
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();

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
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        _createdOrderId = _orderSteps.Response!.OrderId.ToString();
    }

    private async Task The_order_status_is_requested_via_grpc()
    {
        await _grpcSteps.GetOrderStatus(_createdOrderId);
    }

    private void The_grpc_response_should_contain_the_order_details()
    {
        _grpcSteps.OrderStatusReply!.OrderId.Should().Be(_createdOrderId);
        _grpcSteps.OrderStatusReply!.CustomerName.Should().Be(_customerName);
        _grpcSteps.OrderStatusReply!.Status.Should().Be(OrderStatuses.Created);
    }

    private async Task The_order_status_for_a_non_existent_order_is_requested_via_grpc()
    {
        await _grpcSteps.GetOrderStatus(Guid.NewGuid().ToString());
    }

    private void The_grpc_response_should_be_a_not_found_error()
    {
        _grpcSteps.RpcException.Should().NotBeNull();
        _grpcSteps.RpcException!.StatusCode.Should().Be(StatusCode.NotFound);
    }

    #endregion
}
