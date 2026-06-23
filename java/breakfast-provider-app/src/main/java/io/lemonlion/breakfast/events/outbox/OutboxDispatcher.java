package io.lemonlion.breakfast.events.outbox;

import io.lemonlion.breakfast.storage.OutboxMessage;

/** Twin of C# {@code IOutboxDispatcher}: ships a pending outbox message to its destination. */
public interface OutboxDispatcher {

    /** The {@link OutboxDestinations} value this dispatcher handles. */
    String destination();

    void dispatch(OutboxMessage message);
}
