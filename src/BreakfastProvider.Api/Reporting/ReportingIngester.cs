using Microsoft.EntityFrameworkCore;

namespace BreakfastProvider.Api.Reporting;

public class ReportingIngester(IDbContextFactory<ReportingDbContext> dbContextFactory, ILogger<ReportingIngester> logger) : IReportingIngester
{
    public async Task IngestOrderCreatedAsync(Guid orderId, string customerName, int itemCount, int? tableNumber, DateTime createdAt, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.OrderSummaries.FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
        if (existing is not null)
        {
            logger.LogDebug("Order {OrderId} already ingested, skipping", orderId);
            return;
        }

        db.OrderSummaries.Add(new OrderSummary
        {
            OrderId = orderId,
            CustomerName = customerName,
            ItemCount = itemCount,
            TableNumber = tableNumber,
            Status = "Created",
            CreatedAt = createdAt
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Ingested order summary for {OrderId}", orderId);
    }

    public async Task IngestRecipeLogAsync(Guid orderId, string recipeType, List<string> ingredients, List<string> toppings, DateTime loggedAt, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        db.RecipeReports.Add(new RecipeReport
        {
            OrderId = orderId,
            RecipeType = recipeType,
            Ingredients = string.Join(",", ingredients),
            Toppings = string.Join(",", toppings),
            LoggedAt = loggedAt
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Ingested recipe report for order {OrderId}", orderId);
    }

    public async Task IngestBatchCompletionAsync(Guid batchId, string recipeType, List<string> ingredients, DateTime completedAt, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        db.BatchCompletionRecords.Add(new BatchCompletionRecord
        {
            BatchId = batchId,
            RecipeType = recipeType,
            Ingredients = string.Join(",", ingredients),
            CompletedAt = completedAt
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Ingested batch completion for {BatchId}", batchId);
    }

    public async Task IngestIngredientShipmentAsync(Guid deliveryId, string ingredientName, decimal quantity, DateTime deliveredAt, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        db.IngredientShipments.Add(new IngredientShipment
        {
            DeliveryId = deliveryId,
            IngredientName = ingredientName,
            Quantity = quantity,
            DeliveredAt = deliveredAt
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Ingested ingredient shipment {DeliveryId}", deliveryId);
    }

    public async Task IngestEquipmentAlertAsync(Guid alertId, Guid batchId, string equipmentName, string alertType, DateTime alertedAt, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        db.EquipmentAlerts.Add(new EquipmentAlert
        {
            AlertId = alertId,
            BatchId = batchId,
            EquipmentName = equipmentName,
            AlertType = alertType,
            AlertedAt = alertedAt
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Ingested equipment alert {AlertId} for batch {BatchId}", alertId, batchId);
    }
}
