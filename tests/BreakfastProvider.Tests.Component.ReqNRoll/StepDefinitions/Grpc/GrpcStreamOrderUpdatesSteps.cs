using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Grpc.Core;
using Reqnroll;
using TestTrackingDiagrams.ReqNRoll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Grpc;

[Binding]
public class GrpcStreamOrderUpdatesSteps(
    AppManager appManager,
    PostOrderSteps orderSteps)
{
    private readonly GrpcBreakfastSteps _grpcSteps = new();

    private bool _initialized;

    private void EnsureGrpcClient()
    {
        if (_initialized) return;
        _initialized = true;
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest)
            _grpcSteps.InitializeExternal(AppManager.Settings.ExternalServiceUnderTestUrl!);
        else
            _grpcSteps.Initialize(appManager.AppFactory, CurrentTestInfo.Fetcher);
    }

    [When("order updates are streamed via gRPC")]
    public async Task WhenOrderUpdatesAreStreamedViaGrpc()
    {
        EnsureGrpcClient();
        await _grpcSteps.StreamOrderUpdates(orderSteps.Response!.OrderId.ToString());
    }

    [When("order updates for a non-existent order are streamed via gRPC")]
    public async Task WhenOrderUpdatesForANonExistentOrderAreStreamedViaGrpc()
    {
        EnsureGrpcClient();
        await _grpcSteps.StreamOrderUpdates(Guid.NewGuid().ToString());
    }

    [Then("the streamed response should contain the order status")]
    public void ThenTheStreamedResponseShouldContainTheOrderStatus()
    {
        Track.That(() => _grpcSteps.StreamedReplies.Should().HaveCount(1));
        Track.That(() => _grpcSteps.StreamedReplies[0].OrderId.Should().Be(orderSteps.Response!.OrderId.ToString()));
        Track.That(() => _grpcSteps.StreamedReplies[0].CustomerName.Should().Be(orderSteps.Request.CustomerName));
        Track.That(() => _grpcSteps.StreamedReplies[0].Status.Should().Be(OrderStatuses.Created));
    }

    [Then("the gRPC stream should return a not found error")]
    public void ThenTheGrpcStreamShouldReturnANotFoundError()
    {
        Track.That(() => _grpcSteps.RpcException.Should().NotBeNull());
        Track.That(() => _grpcSteps.RpcException!.StatusCode.Should().Be(StatusCode.NotFound));
    }
}
