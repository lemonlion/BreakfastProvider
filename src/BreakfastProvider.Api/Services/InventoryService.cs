using BreakfastProvider.Api.Data;
using BreakfastProvider.Api.Data.Entities;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace BreakfastProvider.Api.Services;

public class InventoryService(
    IDbContextFactory<BreakfastDbContext> dbContextFactory,
    PubSubEventPublisher<InventoryItemAddedEvent> itemAddedPublisher,
    PubSubEventPublisher<InventoryStockUpdatedEvent> stockUpdatedPublisher,
    ILogger<InventoryService> logger) : IInventoryService
{
    public async Task<InventoryItemResponse> CreateAsync(InventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("InventoryService.Create");

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new InventoryItem
        {
            Name = request.Name!,
            Category = request.Category!,
            Quantity = request.Quantity,
            Unit = request.Unit!,
            ReorderLevel = request.ReorderLevel,
            LastRestockedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        db.InventoryItems.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Inventory item '{Name}' created with ID {Id}", entity.Name, entity.Id);

        await itemAddedPublisher.PublishEvent(new InventoryItemAddedEvent
        {
            ItemId = entity.Id,
            Name = entity.Name,
            Category = entity.Category,
            Quantity = entity.Quantity,
            Unit = entity.Unit,
            AddedAt = entity.CreatedAt
        }, cancellationToken);

        return MapToResponse(entity);
    }

    public async Task<InventoryItemResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.InventoryItems.FindAsync([id], cancellationToken);
        return entity is null ? null : MapToResponse(entity);
    }

    public async Task<List<InventoryItemResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var items = await db.InventoryItems.OrderBy(i => i.Name).ToListAsync(cancellationToken);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<InventoryItemResponse?> UpdateAsync(int id, InventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("InventoryService.Update");

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.InventoryItems.FindAsync([id], cancellationToken);
        if (entity is null) return null;

        var previousQuantity = entity.Quantity;

        entity.Name = request.Name!;
        entity.Category = request.Category!;
        entity.Quantity = request.Quantity;
        entity.Unit = request.Unit!;
        entity.ReorderLevel = request.ReorderLevel;
        entity.LastRestockedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Inventory item '{Name}' (ID {Id}) updated", entity.Name, entity.Id);

        if (previousQuantity != entity.Quantity)
        {
            await stockUpdatedPublisher.PublishEvent(new InventoryStockUpdatedEvent
            {
                ItemId = entity.Id,
                Name = entity.Name,
                PreviousQuantity = previousQuantity,
                NewQuantity = entity.Quantity,
                UpdatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.InventoryItems.FindAsync([id], cancellationToken);
        if (entity is null) return false;

        db.InventoryItems.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Inventory item '{Name}' (ID {Id}) deleted", entity.Name, entity.Id);
        return true;
    }

    private static InventoryItemResponse MapToResponse(InventoryItem entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Category = entity.Category,
        Quantity = entity.Quantity,
        Unit = entity.Unit,
        ReorderLevel = entity.ReorderLevel,
        LastRestockedAt = entity.LastRestockedAt,
        CreatedAt = entity.CreatedAt
    };
}
