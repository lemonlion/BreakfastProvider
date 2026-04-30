using BreakfastProvider.Api.Storage;
using Grpc.Core;

namespace BreakfastProvider.Api.Grpc;

public class BreakfastGrpcService(
    ICosmosRepository<OrderDocument> orderRepository,
    ILogger<BreakfastGrpcService> logger) : BreakfastGrpc.BreakfastGrpcBase
{
    public override async Task<RecipeSummaryReply> GetRecipeSummary(RecipeSummaryRequest request, ServerCallContext context)
    {
        logger.LogInformation("gRPC GetRecipeSummary called for {RecipeType}", request.RecipeType);

        var reply = new RecipeSummaryReply
        {
            RecipeType = request.RecipeType,
            TotalBatches = request.RecipeType switch
            {
                "Pancakes" => 42,
                "Waffles" => 28,
                _ => 0
            },
            LastPreparedAt = DateTime.UtcNow.ToString("O")
        };

        reply.CommonIngredients.AddRange(request.RecipeType switch
        {
            "Pancakes" => ["Milk", "Flour", "Eggs"],
            "Waffles" => ["Milk", "Flour", "Eggs", "Butter"],
            _ => []
        });

        return reply;
    }

    public override async Task<OrderStatusReply> GetOrderStatus(OrderStatusRequest request, ServerCallContext context)
    {
        logger.LogInformation("gRPC GetOrderStatus called for {OrderId}", request.OrderId);

        try
        {
            var order = await orderRepository.GetByIdAsync(request.OrderId, request.OrderId, context.CancellationToken);
            return new OrderStatusReply
            {
                OrderId = order.OrderId.ToString(),
                Status = order.Status,
                CustomerName = order.CustomerName,
                ItemCount = order.Items.Count,
                CreatedAt = order.CreatedAt.ToString("O")
            };
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.OrderId} not found"));
        }
    }

    public override async Task StreamOrderUpdates(OrderStatusRequest request, IServerStreamWriter<OrderStatusReply> responseStream, ServerCallContext context)
    {
        logger.LogInformation("gRPC StreamOrderUpdates started for {OrderId}", request.OrderId);

        // Send the current status as the first message
        try
        {
            var order = await orderRepository.GetByIdAsync(request.OrderId, request.OrderId, context.CancellationToken);
            await responseStream.WriteAsync(new OrderStatusReply
            {
                OrderId = order.OrderId.ToString(),
                Status = order.Status,
                CustomerName = order.CustomerName,
                ItemCount = order.Items.Count,
                CreatedAt = order.CreatedAt.ToString("O")
            }, context.CancellationToken);
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.OrderId} not found"));
        }
    }
}
