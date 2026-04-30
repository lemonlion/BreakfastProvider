using Dependencies.Fakes.NotificationService.Grpc;
using Grpc.Core;

namespace Dependencies.Fakes.NotificationService;

public class FakeNotificationGrpcService : NotificationGrpc.NotificationGrpcBase
{
    public override Task<NotificationReply> SendOrderConfirmation(OrderConfirmationRequest request, ServerCallContext context)
    {
        return Task.FromResult(new NotificationReply
        {
            Success = true,
            NotificationId = Guid.NewGuid().ToString(),
            Message = $"Order confirmation sent to {request.CustomerName} for order {request.OrderId}"
        });
    }

    public override Task<NotificationReply> SendReservationReminder(ReservationReminderRequest request, ServerCallContext context)
    {
        return Task.FromResult(new NotificationReply
        {
            Success = true,
            NotificationId = Guid.NewGuid().ToString(),
            Message = $"Reservation reminder sent to {request.CustomerName} for table {request.TableNumber}"
        });
    }
}
