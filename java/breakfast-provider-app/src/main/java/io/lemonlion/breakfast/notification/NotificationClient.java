package io.lemonlion.breakfast.notification;

import java.time.Instant;
import java.util.UUID;

/** Twin of C# {@code INotificationClient} (gRPC notification service). Fire-and-forget calls. */
public interface NotificationClient {

    NotificationResult sendOrderConfirmation(UUID orderId, String customerName, int itemCount);

    NotificationResult sendReservationReminder(String reservationId, String customerName, Instant reservedAt,
                                               int tableNumber);

    record NotificationResult(boolean success, String notificationId) {
    }
}
