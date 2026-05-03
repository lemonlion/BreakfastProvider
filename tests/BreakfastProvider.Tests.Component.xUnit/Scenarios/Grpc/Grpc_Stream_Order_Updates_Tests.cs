using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Grpc.Core;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Grpc;

public class Grpc_Stream_Order_Updates_Tests : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GrpcBreakfastSteps _grpcSteps;

    public Grpc_Stream_Order_Updates_Tests()
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
    public async Task Streaming_order_updates_should_return_the_current_status()
    {
        // Given a pancake batch has been created
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
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();

        // And an order has been created for the batch
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
        Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _orderSteps.ParseResponse();
        var createdOrderId = _orderSteps.Response!.OrderId;

        // When order updates are streamed via gRPC
        await _grpcSteps.StreamOrderUpdates(createdOrderId.ToString());

        // Then the streamed response should contain the order status
        Track.That(() => _grpcSteps.StreamedReplies.Should().HaveCount(1));
        Track.That(() => _grpcSteps.StreamedReplies[0].OrderId.Should().Be(createdOrderId.ToString()));
        Track.That(() => _grpcSteps.StreamedReplies[0].CustomerName.Should().Be(_customerName));
        Track.That(() => _grpcSteps.StreamedReplies[0].Status.Should().Be(OrderStatuses.Created));
    }

    [Fact]
    public async Task Streaming_updates_for_non_existent_order_should_return_not_found()
    {
        // When order updates for a non-existent order are streamed via gRPC
        await _grpcSteps.StreamOrderUpdates(Guid.NewGuid().ToString());

        // Then the gRPC stream should return a not-found error
        Track.That(() => _grpcSteps.RpcException.Should().NotBeNull());
        Track.That(() => _grpcSteps.RpcException!.StatusCode.Should().Be(StatusCode.NotFound));
    }
}
