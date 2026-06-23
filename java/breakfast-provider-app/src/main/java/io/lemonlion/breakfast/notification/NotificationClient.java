package io.lemonlion.breakfast.notification;

import java.util.UUID;

/** Twin of C# {@code INotificationClient} (gRPC notification service). Fire-and-forget on order creation. */
public interface NotificationClient {

    NotificationResult sendOrderConfirmation(UUID orderId, String customerName, int itemCount);

    record NotificationResult(boolean success, String notificationId) {
    }
}
