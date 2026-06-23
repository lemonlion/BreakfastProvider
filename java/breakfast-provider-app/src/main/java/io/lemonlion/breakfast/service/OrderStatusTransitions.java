package io.lemonlion.breakfast.service;

import java.util.List;
import java.util.Map;

/** Twin of C# {@code OrderService} transition table: which status changes are permitted. */
public final class OrderStatusTransitions {

    public static final String CREATED = "Created";
    public static final String PREPARING = "Preparing";
    public static final String READY = "Ready";
    public static final String COMPLETED = "Completed";
    public static final String CANCELLED = "Cancelled";

    private static final Map<String, List<String>> ALLOWED = Map.of(
            CREATED, List.of(PREPARING, CANCELLED),
            PREPARING, List.of(READY),
            READY, List.of(COMPLETED));

    private OrderStatusTransitions() {
    }

    public static boolean isValid(String from, String to) {
        return ALLOWED.getOrDefault(from, List.of()).contains(to);
    }
}
