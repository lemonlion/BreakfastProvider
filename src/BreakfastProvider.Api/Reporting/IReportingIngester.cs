namespace BreakfastProvider.Api.Reporting;

public interface IReportingIngester
{
    Task IngestOrderCreatedAsync(Guid orderId, string customerName, int itemCount, int? tableNumber, DateTime createdAt, CancellationToken cancellationToken = default);
    Task IngestRecipeLogAsync(Guid orderId, string recipeType, List<string> ingredients, List<string> toppings, DateTime loggedAt, CancellationToken cancellationToken = default);
    Task IngestBatchCompletionAsync(Guid batchId, string recipeType, List<string> ingredients, DateTime completedAt, CancellationToken cancellationToken = default);
    Task IngestIngredientShipmentAsync(Guid deliveryId, string ingredientName, decimal quantity, DateTime deliveredAt, CancellationToken cancellationToken = default);
    Task IngestEquipmentAlertAsync(Guid alertId, Guid batchId, string equipmentName, string alertType, DateTime alertedAt, CancellationToken cancellationToken = default);
}
