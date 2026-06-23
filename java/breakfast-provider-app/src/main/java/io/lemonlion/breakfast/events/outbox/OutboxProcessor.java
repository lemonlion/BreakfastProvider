package io.lemonlion.breakfast.events.outbox;

import io.lemonlion.breakfast.config.OutboxConfig;
import io.lemonlion.breakfast.storage.OutboxMessage;
import io.lemonlion.breakfast.storage.OutboxMessageStatus;
import java.time.Instant;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

/**
 * Twin of C# {@code OutboxProcessor}: a polling background worker that dispatches pending outbox
 * messages to their destination, retrying up to {@code maxRetryCount} before marking them failed.
 */
@Component
public class OutboxProcessor {

    private static final Logger log = LoggerFactory.getLogger(OutboxProcessor.class);

    private final OutboxStore outboxStore;
    private final OutboxConfig config;
    private final Map<String, OutboxDispatcher> dispatchers;

    public OutboxProcessor(OutboxStore outboxStore, OutboxConfig config, List<OutboxDispatcher> dispatchers) {
        this.outboxStore = outboxStore;
        this.config = config;
        this.dispatchers = dispatchers.stream()
                .collect(Collectors.toMap(OutboxDispatcher::destination, Function.identity()));
    }

    @Scheduled(fixedDelayString = "#{ ${outbox.polling-interval-seconds:5} * 1000 }")
    public void process() {
        if (!config.isEnabled()) {
            return;
        }
        List<OutboxMessage> pending;
        try {
            pending = outboxStore.findByStatus(OutboxMessageStatus.PENDING, config.getBatchSize());
        } catch (RuntimeException e) {
            log.warn("Outbox poll failed", e);
            return;
        }
        for (OutboxMessage message : pending) {
            dispatchOne(message);
        }
    }

    private void dispatchOne(OutboxMessage message) {
        OutboxDispatcher dispatcher = dispatchers.get(message.getDestination());
        try {
            if (dispatcher == null) {
                throw new IllegalStateException("No dispatcher for destination " + message.getDestination());
            }
            dispatcher.dispatch(message);
            message.setStatus(OutboxMessageStatus.PROCESSED);
            message.setProcessedAt(Instant.now());
        } catch (RuntimeException e) {
            message.setRetryCount(message.getRetryCount() + 1);
            message.setErrorMessage(e.getMessage());
            if (message.getRetryCount() >= config.getMaxRetryCount()) {
                message.setStatus(OutboxMessageStatus.FAILED);
                log.warn("Outbox message {} failed permanently after {} retries", message.getId(), message.getRetryCount());
            }
        }
        outboxStore.update(message);
    }
}
