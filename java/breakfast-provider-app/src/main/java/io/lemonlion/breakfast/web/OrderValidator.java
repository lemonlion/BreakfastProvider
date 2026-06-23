package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.config.OrderConfig;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.request.UpdateOrderStatusRequest;
import java.util.List;
import org.springframework.stereotype.Component;

/** Twin of C# {@code OrderRequestValidator} + {@code UpdateOrderStatusRequestValidator}. */
@Component
public class OrderValidator {

    private static final List<String> ALLOWED_STATUSES = List.of("Preparing", "Ready", "Completed", "Cancelled");

    private final OrderConfig orderConfig;

    public OrderValidator(OrderConfig orderConfig) {
        this.orderConfig = orderConfig;
    }

    /** Throws {@link ValidationException} if the order request is invalid. */
    public void validate(OrderRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();

        if (isBlank(request.customerName())) {
            errors.add("CustomerName", "'Customer Name' is required.");
        } else if (Xss.containsHtmlOrScript(request.customerName())) {
            errors.add("CustomerName", "'Customer Name' must not contain HTML or script content.");
        }

        List<OrderItemRequest> items = request.items();
        if (items == null || items.isEmpty()) {
            errors.add("Items", "The Items field is required.");
        } else {
            int max = orderConfig.getMaxItemsPerOrder();
            if (items.size() > max) {
                errors.add("Items", "An order cannot contain more than " + max + " items.");
            }
            for (int i = 0; i < items.size(); i++) {
                OrderItemRequest item = items.get(i);
                String prefix = "Items[" + i + "].";
                if (isBlank(item.itemType())) {
                    errors.add(prefix + "ItemType", "'Item Type' is required.");
                } else if (Xss.containsHtmlOrScript(item.itemType())) {
                    errors.add(prefix + "ItemType", "'Item Type' must not contain HTML or script content.");
                }
                if (item.batchId() == null) {
                    errors.add(prefix + "BatchId", "'Batch Id' is required.");
                }
                if (item.quantity() != null && item.quantity() <= 0) {
                    errors.add(prefix + "Quantity", "Quantity must be greater than zero.");
                }
            }
        }

        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }

    /** Throws {@link ValidationException} if the status-update request is invalid. */
    public void validate(UpdateOrderStatusRequest request) {
        ValidationException.Collector errors = new ValidationException.Collector();
        if (isBlank(request.status())) {
            errors.add("Status", "'Status' is required.");
        } else if (!ALLOWED_STATUSES.contains(request.status())) {
            errors.add("Status", "'Status' must be one of: " + String.join(", ", ALLOWED_STATUSES) + ".");
        }
        if (errors.hasErrors()) {
            throw new ValidationException(errors.build());
        }
    }

    private static boolean isBlank(String value) {
        return value == null || value.isBlank();
    }
}
