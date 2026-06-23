package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.model.response.PaginatedResponse;
import java.util.Optional;
import java.util.UUID;

/** Twin of C# {@code IOrderService}. */
public interface OrderService {

    OrderResponse createOrder(OrderRequest request);

    Optional<OrderResponse> getOrder(UUID orderId);

    PaginatedResponse<OrderResponse> listOrders(int page, int pageSize);

    /**
     * Updates an order's status.
     *
     * @return a result that is either not-found, an invalid-transition error, or the updated order.
     */
    StatusUpdateResult updateOrderStatus(UUID orderId, String newStatus);

    /** Outcome of {@link #updateOrderStatus}: exactly one of the three states holds. */
    record StatusUpdateResult(OrderResponse order, String error, boolean notFound) {

        public static StatusUpdateResult ok(OrderResponse order) {
            return new StatusUpdateResult(order, null, false);
        }

        public static StatusUpdateResult invalid(String error) {
            return new StatusUpdateResult(null, error, false);
        }

        public static StatusUpdateResult notFoundResult() {
            return new StatusUpdateResult(null, null, true);
        }
    }
}
