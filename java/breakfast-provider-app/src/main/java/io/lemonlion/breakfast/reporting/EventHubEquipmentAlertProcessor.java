package io.lemonlion.breakfast.reporting;

import com.azure.messaging.eventhubs.EventProcessorClient;
import com.azure.messaging.eventhubs.EventProcessorClientBuilder;
import com.azure.messaging.eventhubs.checkpointstore.blob.BlobCheckpointStore;
import com.azure.messaging.eventhubs.models.ErrorContext;
import com.azure.messaging.eventhubs.models.EventContext;
import com.azure.storage.blob.BlobContainerAsyncClient;
import com.azure.storage.blob.BlobContainerClientBuilder;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.lemonlion.breakfast.config.EventHubConfig;
import io.lemonlion.breakfast.model.event.EquipmentAlertEvent;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.SmartLifecycle;
import org.springframework.stereotype.Component;

/**
 * Twin of the C# {@code EventHubEquipmentAlertConsumerService}: an {@link EventProcessorClient} that
 * consumes equipment-alert events from Azure Event Hubs (checkpointing to Azure Blob storage / Azurite)
 * and hands each to {@link EquipmentAlertConsumer#ingest}. Disabled (does not start) when no Event Hubs
 * connection string is configured.
 */
@Component
@ConditionalOnProperty(name = "breakfast.background-consumers.enabled", matchIfMissing = true)
public class EventHubEquipmentAlertProcessor implements SmartLifecycle {

    private static final Logger log = LoggerFactory.getLogger(EventHubEquipmentAlertProcessor.class);

    private final EventHubConfig config;
    private final EquipmentAlertConsumer consumer;
    private final ObjectMapper objectMapper;

    private EventProcessorClient processor;
    private volatile boolean running;

    public EventHubEquipmentAlertProcessor(EventHubConfig config, EquipmentAlertConsumer consumer,
                                           ObjectMapper objectMapper) {
        this.config = config;
        this.consumer = consumer;
        this.objectMapper = objectMapper;
    }

    @Override
    public void start() {
        if (running || !config.isEnabled()) {
            return;
        }
        // The checkpoint-store blob container must exist before the processor starts.
        BlobContainerAsyncClient containerClient = new BlobContainerClientBuilder()
                .connectionString(config.getBlobConnectionString())
                .containerName(config.getCheckpointContainer())
                .buildAsyncClient();
        containerClient.createIfNotExists().block();

        processor = new EventProcessorClientBuilder()
                .connectionString(config.getConnectionString(), config.getEventHubName())
                .consumerGroup(config.getConsumerGroup())
                .checkpointStore(new BlobCheckpointStore(containerClient))
                .processEvent(this::onEvent)
                .processError(this::onError)
                .buildEventProcessorClient();
        processor.start();
        running = true;
        log.info("Event Hubs equipment-alert processor started on hub {}", config.getEventHubName());
    }

    private void onEvent(EventContext context) {
        try {
            EquipmentAlertEvent event = objectMapper.readValue(
                    context.getEventData().getBodyAsString(), EquipmentAlertEvent.class);
            consumer.ingest(event);
            context.updateCheckpoint();
        } catch (Exception e) {
            log.error("Failed to process equipment-alert event", e);
        }
    }

    private void onError(ErrorContext context) {
        log.error("Event Hubs processor error on partition {}",
                context.getPartitionContext().getPartitionId(), context.getThrowable());
    }

    @Override
    public void stop() {
        if (processor != null) {
            processor.stop();
        }
        running = false;
    }

    @Override
    public boolean isRunning() {
        return running;
    }
}
