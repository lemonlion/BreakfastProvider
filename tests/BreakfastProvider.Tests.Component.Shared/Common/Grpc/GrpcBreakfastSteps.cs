using BreakfastProvider.Api;
using BreakfastProvider.Api.Grpc;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using TestTrackingDiagrams.Extensions.Grpc;

namespace BreakfastProvider.Tests.Component.Shared.Common.Grpc;

public class GrpcBreakfastSteps
{
    private BreakfastGrpc.BreakfastGrpcClient? _client;

    public RecipeSummaryReply? RecipeSummaryReply { get; private set; }
    public OrderStatusReply? OrderStatusReply { get; private set; }
    public List<OrderStatusReply> StreamedReplies { get; } = [];
    public RpcException? RpcException { get; private set; }

    public void Initialize<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory, Func<(string Name, string Id)> currentTestInfoFetcher) where TEntryPoint : class
    {
        _client = factory.CreateTestTrackingGrpcClient<TEntryPoint, BreakfastGrpc.BreakfastGrpcClient>(
            new GrpcTrackingOptions
            {
                ServiceName = Documentation.ServiceNames.BreakfastProvider,
                Verbosity = GrpcTrackingVerbosity.Detailed,
                CurrentTestInfoFetcher = currentTestInfoFetcher
            });
    }

    public async Task GetRecipeSummary(string recipeType)
    {
        RecipeSummaryReply = await _client!.GetRecipeSummaryAsync(
            new RecipeSummaryRequest { RecipeType = recipeType });
    }

    public async Task GetOrderStatus(string orderId)
    {
        try
        {
            OrderStatusReply = await _client!.GetOrderStatusAsync(
                new OrderStatusRequest { OrderId = orderId });
        }
        catch (RpcException ex)
        {
            RpcException = ex;
        }
    }

    public async Task StreamOrderUpdates(string orderId)
    {
        try
        {
            using var call = _client!.StreamOrderUpdates(
                new OrderStatusRequest { OrderId = orderId });
            await foreach (var reply in call.ResponseStream.ReadAllAsync())
            {
                StreamedReplies.Add(reply);
            }
        }
        catch (RpcException ex)
        {
            RpcException = ex;
        }
    }
}
