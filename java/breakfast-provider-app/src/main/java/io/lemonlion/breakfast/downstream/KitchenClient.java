package io.lemonlion.breakfast.downstream;

import io.lemonlion.breakfast.model.request.OrderRequest;
import java.util.UUID;

/**
 * Twin of the C# Kitchen Service HTTP integration. Fire-and-forget: a failure must not roll back an
 * already-committed order (the impl logs and swallows transport errors).
 */
public interface KitchenClient {

    void requestPreparation(UUID orderId, OrderRequest order);
}
