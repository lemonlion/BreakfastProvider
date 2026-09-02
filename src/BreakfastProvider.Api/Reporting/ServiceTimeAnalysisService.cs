using BreakfastProvider.Api.Data.ClickHouse;
using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Services;
using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Reporting;

public interface IServiceTimeAnalysisService
{
    Task ProcessOrderServedAsync(Guid serviceId, Guid orderId, string itemType, decimal waitSeconds, DateTime servedAt, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles <c>OrderServedEvent</c>s: stores the service time in ClickHouse, sends a gRPC
/// notification, then asks the Kitchen Service for the order's status.
/// </summary>
public class ServiceTimeAnalysisService(
    IClickHouseConnectionFactory connectionFactory,
    INotificationClient notificationClient,
    IHttpClientFactory httpClientFactory,
    ILogger<ServiceTimeAnalysisService> logger) : IServiceTimeAnalysisService
{
    private const string TableName = "service_times";

    public async Task ProcessOrderServedAsync(Guid serviceId, Guid orderId, string itemType, decimal waitSeconds, DateTime servedAt, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("ServiceTimeAnalysisService.ProcessOrderServed");

        // ClickHouse DateTime parameters are sent without a zone and interpreted in the server's
        // timezone (UTC), so normalise the incoming timestamp to UTC first.
        var servedAtUtc = servedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(servedAt, DateTimeKind.Utc)
            : servedAt.ToUniversalTime();

        // Step 1: Write to ClickHouse
        await using (var connection = connectionFactory.CreateConnection())
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText =
                $"INSERT INTO {TableName} (service_id, order_id, item_type, wait_seconds, served_at) " +
                "VALUES ({serviceId:String}, {orderId:String}, {itemType:String}, {waitSeconds:Float64}, {servedAt:DateTime})";
            command
                .AddParameter("serviceId", serviceId.ToString())
                .AddParameter("orderId", orderId.ToString())
                .AddParameter("itemType", itemType)
                .AddParameter("waitSeconds", (double)waitSeconds)
                .AddParameter("servedAt", servedAtUtc);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("Service time {ServiceId} stored in ClickHouse for order {OrderId}", serviceId, orderId);

        // Step 2: Send notification via gRPC
        var (success, notificationId) = await notificationClient.SendOrderConfirmationAsync(
            orderId.ToString(), itemType, 1, cancellationToken);

        logger.LogInformation("Notification sent for served order {OrderId}: Success={Success}, NotificationId={NotificationId}",
            orderId, success, notificationId);

        // Step 3: Ask the Kitchen Service for the order's status
        var kitchenClient = httpClientFactory.CreateClient(HttpClientNames.KitchenService);
        await kitchenClient.GetAsync($"status/{orderId}", cancellationToken);

        logger.LogInformation("Kitchen service status requested for served order {OrderId}", orderId);
    }
}
