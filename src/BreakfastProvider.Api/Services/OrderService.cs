using System.Collections.Frozen;
using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Api.Telemetry;

using BreakfastProvider.Api.Reporting;

namespace BreakfastProvider.Api.Services;

public class OrderService(
    ICosmosRepository<OrderDocument> orderRepository,
    ICosmosRepository<AuditLogDocument> auditLogRepository,
    IOutboxWriter outboxWriter,
    IHttpClientFactory httpClientFactory,
    IReportingIngester reportingIngester,
    INotificationClient notificationClient,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<OrderResponse> CreateOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("OrderService.CreateOrder");

        var orderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        activity?.SetTag("order.id", orderId.ToString());
        activity?.SetTag("order.customer_name", request.CustomerName);
        activity?.SetTag("order.item_count", request.Items.Count);
        activity?.SetTag("order.table_number", request.TableNumber);

        var document = new OrderDocument
        {
            Id = orderId.ToString(),
            PartitionKey = orderId.ToString(),
            OrderId = orderId,
            CustomerName = request.CustomerName!,
            Items = request.Items.Select(i => new OrderItemDocument
            {
                ItemType = i.ItemType!,
                BatchId = i.BatchId!.Value,
                Quantity = i.Quantity
            }).ToList(),
            TableNumber = request.TableNumber,
            Status = "Created",
            CreatedAt = now
        };

        var @event = new OrderCreatedEvent
        {
            OrderId = orderId,
            CustomerName = request.CustomerName!,
            ItemCount = request.Items.Count,
            TableNumber = request.TableNumber,
            CreatedAt = now
        };

        // Atomically write the order document and outbox message in a single transactional batch.
        await outboxWriter.WriteAsync(document, @event, document.PartitionKey, OutboxDestinations.EventGrid, cancellationToken);

        logger.LogInformation("Order {OrderId} created for customer {CustomerName} with {ItemCount} items at table {TableNumber}",
            orderId, request.CustomerName, request.Items.Count, request.TableNumber);

        // Log audit entry
        var auditDoc = new AuditLogDocument
        {
            PartitionKey = "Order",
            AuditLogId = Guid.NewGuid(),
            Action = "Created",
            EntityType = "Order",
            EntityId = orderId,
            Details = $"Order created for {request.CustomerName} with {request.Items.Count} items",
            Timestamp = now
        };
        await auditLogRepository.CreateAsync(auditDoc, auditDoc.PartitionKey, cancellationToken);

        // Send to kitchen — fire-and-forget; order is already persisted.
        try
        {
            var kitchenClient = httpClientFactory.CreateClient(HttpClientNames.KitchenService);
            await kitchenClient.PostAsJsonAsync("prepare", new
            {
                OrderId = orderId,
                Items = request.Items.Select(i => new { i.ItemType, i.Quantity })
            }, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Kitchen notification failed for order {OrderId}; order is committed", orderId);
        }

        // Send confirmation notification via gRPC — fire-and-forget.
        try
        {
            await notificationClient.SendOrderConfirmationAsync(
                orderId.ToString(), request.CustomerName!, request.Items.Count, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Order confirmation notification failed for order {OrderId}; order is committed", orderId);
        }

        // Ingest into reporting database — fire-and-forget; order is already committed.
        try
        {
            await reportingIngester.IngestOrderCreatedAsync(orderId, request.CustomerName!, request.Items.Count, request.TableNumber, now, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reporting ingestion failed for order {OrderId}; order is committed", orderId);
        }

        DiagnosticsConfig.OrdersCreated.Add(1,
            new KeyValuePair<string, object?>("order.table_number", request.TableNumber));

        return new OrderResponse
        {
            OrderId = orderId,
            CustomerName = request.CustomerName!,
            Items = request.Items.Select(i => new OrderItemResponse
            {
                ItemType = i.ItemType!,
                BatchId = i.BatchId!.Value,
                Quantity = i.Quantity
            }).ToList(),
            TableNumber = request.TableNumber,
            Status = "Created",
            CreatedAt = now
        };
    }

    public async Task<OrderResponse?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var document = await orderRepository.GetByIdAsync(orderId.ToString(), orderId.ToString(), cancellationToken);
        if (document == null) return null;

        return MapToResponse(document);
    }

    public async Task<PaginatedResponse<OrderResponse>> ListOrdersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        var (documents, totalCount) = await orderRepository.QueryPagedAsync(
            o => true,
            offset,
            pageSize,
            cancellationToken);

        var items = documents
            .OrderByDescending(o => o.CreatedAt)
            .Select(MapToResponse)
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<OrderResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<(OrderResponse? Order, string? Error)> UpdateOrderStatusAsync(Guid orderId, string newStatus, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("OrderService.UpdateOrderStatus");
        activity?.SetTag("order.id", orderId.ToString());
        activity?.SetTag("order.new_status", newStatus);

        var document = await orderRepository.GetByIdAsync(orderId.ToString(), orderId.ToString(), cancellationToken);
        if (document == null) return (null, null);

        if (!IsValidTransition(document.Status, newStatus))
        {
            activity?.SetTag("order.transition_valid", false);
            return (null, $"Cannot transition from '{document.Status}' to '{newStatus}'.");
        }

        var previousStatus = document.Status;
        document.Status = newStatus;
        await orderRepository.UpsertAsync(document, document.PartitionKey, cancellationToken);

        var auditDoc = new AuditLogDocument
        {
            PartitionKey = "Order",
            AuditLogId = Guid.NewGuid(),
            Action = "StatusChanged",
            EntityType = "Order",
            EntityId = orderId,
            Details = $"Order status changed from {previousStatus} to {newStatus}",
            Timestamp = DateTime.UtcNow
        };
        await auditLogRepository.CreateAsync(auditDoc, auditDoc.PartitionKey, cancellationToken);

        activity?.SetTag("order.previous_status", previousStatus);

        DiagnosticsConfig.OrderStatusChanged.Add(1,
            new KeyValuePair<string, object?>("order.previous_status", previousStatus),
            new KeyValuePair<string, object?>("order.new_status", newStatus));

        logger.LogInformation("Order {OrderId} status changed from {PreviousStatus} to {NewStatus}",
            orderId, previousStatus, newStatus);

        return (MapToResponse(document), null);
    }

    private static readonly FrozenDictionary<string, FrozenSet<string>> ValidTransitions = new Dictionary<string, FrozenSet<string>>
    {
        ["Created"] = FrozenSet.ToFrozenSet(["Preparing", "Cancelled"]),
        ["Preparing"] = FrozenSet.ToFrozenSet(["Ready"]),
        ["Ready"] = FrozenSet.ToFrozenSet(["Completed"]),
    }.ToFrozenDictionary();

    private static bool IsValidTransition(string currentStatus, string newStatus)
        => ValidTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(newStatus);

    private static OrderResponse MapToResponse(OrderDocument document) => new()
    {
        OrderId = document.OrderId,
        CustomerName = document.CustomerName,
        Items = document.Items.Select(i => new OrderItemResponse
        {
            ItemType = i.ItemType,
            BatchId = i.BatchId,
            Quantity = i.Quantity
        }).ToList(),
        TableNumber = document.TableNumber,
        Status = document.Status,
        CreatedAt = document.CreatedAt
    };
}
