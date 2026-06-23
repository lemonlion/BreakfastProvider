package io.lemonlion.breakfast.events;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

/**
 * Placeholder Event Hubs publisher. There is no broadly-available Event Hubs emulator; the faithful
 * Azure implementation is wired when the run modes / reporting consumers land. Until then this logs.
 */
@Component
public class LoggingEventHubPublisher implements EventHubPublisher {

    private static final Logger log = LoggerFactory.getLogger(LoggingEventHubPublisher.class);

    @Override
    public void publish(Object event) {
        log.info("Event Hubs publish: {}", event);
    }
}
