package io.lemonlion.breakfast.model.request;

import java.util.ArrayList;
import java.util.List;

/** Twin of C# {@code OrderRequest}. Validation is performed programmatically (see {@code OrderValidator}). */
public record OrderRequest(String customerName, List<OrderItemRequest> items, Integer tableNumber) {

    public OrderRequest {
        if (items == null) {
            items = new ArrayList<>();
        }
    }
}
