package io.lemonlion.breakfast.notification;

import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

/**
 * Placeholder notification client. The faithful gRPC implementation (twin of C#
 * {@code GrpcNotificationClient}) arrives in Phase 2 with the {@code NotificationService} fake and
 * generated protobuf stubs; until then this logs so order creation has a confirmation step.
 */
@Component
public class LoggingNotificationClient implements NotificationClient {

    private static final Logger log = LoggerFactory.getLogger(LoggingNotificationClient.class);

    @Override
    public NotificationResult sendOrderConfirmation(UUID orderId, String customerName, int itemCount) {
        String notificationId = UUID.randomUUID().toString();
        log.info("Order confirmation notification for {} to {} ({} items): {}",
                orderId, customerName, itemCount, notificationId);
        return new NotificationResult(true, notificationId);
    }
}
