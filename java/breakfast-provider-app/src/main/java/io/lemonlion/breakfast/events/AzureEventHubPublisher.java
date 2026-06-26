package io.lemonlion.breakfast.events;

import com.azure.messaging.eventhubs.EventData;
import com.azure.messaging.eventhubs.EventDataBatch;
import com.azure.messaging.eventhubs.EventHubClientBuilder;
import com.azure.messaging.eventhubs.EventHubProducerClient;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.lemonlion.breakfast.config.EventHubConfig;
import jakarta.annotation.PreDestroy;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Primary;
import org.springframework.stereotype.Component;

/**
 * Twin of the C# {@code EventHubEventPublisher<T>}: publishes domain events (equipment alerts) to Azure
 * Event Hubs. When no connection string is configured it falls back to logging, so the SUT still runs
 * without an Event Hubs backend; the component tests point it at the Event Hubs emulator.
 */
@Primary
@Component
public class AzureEventHubPublisher implements EventHubPublisher {

    private static final Logger log = LoggerFactory.getLogger(AzureEventHubPublisher.class);

    private final EventHubConfig config;
    private final ObjectMapper objectMapper;

    private volatile EventHubProducerClient producer;

    public AzureEventHubPublisher(EventHubConfig config, ObjectMapper objectMapper) {
        this.config = config;
        this.objectMapper = objectMapper;
    }

    @Override
    public void publish(Object event) {
        if (!config.isEnabled()) {
            log.info("Event Hubs publish (disabled): {}", event);
            return;
        }
        try {
            String json = objectMapper.writeValueAsString(event);
            EventHubProducerClient client = producer();
            EventDataBatch batch = client.createBatch();
            batch.tryAdd(new EventData(json));
            client.send(batch);
        } catch (Exception e) {
            log.warn("Failed to publish event to Event Hubs: {}", event, e);
        }
    }

    private EventHubProducerClient producer() {
        EventHubProducerClient local = producer;
        if (local == null) {
            synchronized (this) {
                local = producer;
                if (local == null) {
                    local = new EventHubClientBuilder()
                            .connectionString(config.getConnectionString(), config.getEventHubName())
                            .buildProducerClient();
                    producer = local;
                }
            }
        }
        return local;
    }

    @PreDestroy
    public synchronized void close() {
        if (producer != null) {
            producer.close();
        }
    }
}
