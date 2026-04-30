using BreakfastProvider.Api.Telemetry;

namespace BreakfastProvider.Api.Services;

public interface INotificationClient
{
    Task<(bool Success, string NotificationId)> SendOrderConfirmationAsync(string orderId, string customerName, int itemCount, CancellationToken cancellationToken = default);
    Task<(bool Success, string NotificationId)> SendReservationReminderAsync(string reservationId, string customerName, DateTime reservedAt, int tableNumber, CancellationToken cancellationToken = default);
}

public class GrpcNotificationClient(Grpc.NotificationGrpc.NotificationGrpcClient client, ILogger<GrpcNotificationClient> logger) : INotificationClient
{
    public async Task<(bool Success, string NotificationId)> SendOrderConfirmationAsync(string orderId, string customerName, int itemCount, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("NotificationClient.SendOrderConfirmation");

        logger.LogInformation("Sending order confirmation notification for {OrderId} to {CustomerName}", orderId, customerName);

        var reply = await client.SendOrderConfirmationAsync(new Grpc.OrderConfirmationRequest
        {
            OrderId = orderId,
            CustomerName = customerName,
            ItemCount = itemCount,
            Channel = "email"
        }, cancellationToken: cancellationToken);

        logger.LogInformation("Order confirmation notification sent: {NotificationId}, Success: {Success}", reply.NotificationId, reply.Success);

        return (reply.Success, reply.NotificationId);
    }

    public async Task<(bool Success, string NotificationId)> SendReservationReminderAsync(string reservationId, string customerName, DateTime reservedAt, int tableNumber, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("NotificationClient.SendReservationReminder");

        logger.LogInformation("Sending reservation reminder for {ReservationId} to {CustomerName}", reservationId, customerName);

        var reply = await client.SendReservationReminderAsync(new Grpc.ReservationReminderRequest
        {
            ReservationId = reservationId,
            CustomerName = customerName,
            ReservedAt = reservedAt.ToString("O"),
            TableNumber = tableNumber
        }, cancellationToken: cancellationToken);

        logger.LogInformation("Reservation reminder sent: {NotificationId}, Success: {Success}", reply.NotificationId, reply.Success);

        return (reply.Success, reply.NotificationId);
    }
}
