package io.lemonlion.breakfast.events;

import java.util.UUID;

/** Twin of the C# recipe-log Kafka publisher: emits a recipe-log event when an order is created. */
public interface RecipeLogPublisher {

    void publishOrderRecipeLog(UUID orderId, String customerName, int itemCount);
}
