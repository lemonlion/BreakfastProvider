package io.lemonlion.breakfast.events;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

/**
 * Placeholder Pub/Sub publisher. The faithful Google Cloud Pub/Sub implementation (with a Testcontainers
 * Pub/Sub emulator) lands with the Reporting/consumer work; until then this logs the published event.
 */
@Component
public class LoggingPubSubPublisher implements PubSubPublisher {

    private static final Logger log = LoggerFactory.getLogger(LoggingPubSubPublisher.class);

    @Override
    public void publish(Object event) {
        log.info("Pub/Sub publish: {}", event);
    }
}
