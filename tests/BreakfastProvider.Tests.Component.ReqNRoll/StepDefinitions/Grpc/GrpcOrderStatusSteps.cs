using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Grpc.Core;
using Reqnroll;
using TestTrackingDiagrams.ReqNRoll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Grpc;

[Binding]
public class GrpcOrderStatusSteps(
    AppManager appManager,
    PostOrderSteps orderSteps)
{
    private readonly GrpcBreakfastSteps _grpcSteps = new();

    private void EnsureGrpcClient()
    {
        if (!AppManager.Settings.RunAgainstExternalServiceUnderTest)
            _grpcSteps.Initialize(appManager.AppFactory, CurrentTestInfo.Fetcher);
    }

    [When("the order status is requested via gRPC")]
    public async Task WhenTheOrderStatusIsRequestedViaGrpc()
    {
        EnsureGrpcClient();
        await _grpcSteps.GetOrderStatus(orderSteps.Response!.OrderId.ToString());
    }

    [When("the order status for a non-existent order is requested via gRPC")]
    public async Task WhenTheOrderStatusForANonExistentOrderIsRequestedViaGrpc()
    {
        EnsureGrpcClient();
        await _grpcSteps.GetOrderStatus(Guid.NewGuid().ToString());
    }

    [Then("the gRPC response should contain the order details")]
    public void ThenTheGrpcResponseShouldContainTheOrderDetails()
    {
        Track.That(() => _grpcSteps.OrderStatusReply.Should().NotBeNull());
        Track.That(() => _grpcSteps.OrderStatusReply!.OrderId.Should().Be(orderSteps.Response!.OrderId.ToString()));
        Track.That(() => _grpcSteps.OrderStatusReply.CustomerName.Should().Be(orderSteps.Request.CustomerName));
        Track.That(() => _grpcSteps.OrderStatusReply.Status.Should().Be(OrderStatuses.Created));
    }

    [Then("the gRPC response should be a not found error")]
    public void ThenTheGrpcResponseShouldBeANotFoundError()
    {
        Track.That(() => _grpcSteps.RpcException.Should().NotBeNull());
        Track.That(() => _grpcSteps.RpcException!.StatusCode.Should().Be(StatusCode.NotFound));
    }
}
