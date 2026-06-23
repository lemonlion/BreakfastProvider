package io.lemonlion.breakfast.events.outbox;

import io.lemonlion.breakfast.storage.OutboxMessage;
import java.util.List;

/** Read/update access to outbox messages for the processor and tests (twin of the C# outbox repository). */
public interface OutboxStore {

    List<OutboxMessage> findByStatus(String status, int limit);

    List<OutboxMessage> findAll();

    /** Persists an in-place status/retry update for an existing message. */
    OutboxMessage update(OutboxMessage message);

    /** Inserts a message directly (used by tests to seed pending/failing messages). */
    OutboxMessage add(OutboxMessage message);
}
