package io.lemonlion.breakfast.reporting;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.google.cloud.pubsub.v1.AckReplyConsumer;
import com.google.cloud.pubsub.v1.MessageReceiver;
import com.google.cloud.pubsub.v1.Subscriber;
import com.google.pubsub.v1.ProjectSubscriptionName;
import io.lemonlion.breakfast.config.PubSubConfig;
import io.lemonlion.breakfast.persistence.pubsub.PubSubSupport;
import io.lemonlion.breakfast.storage.BatchCompletionRecordEntity;
import io.lemonlion.breakfast.storage.BatchCompletionRecordRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.SmartLifecycle;
import org.springframework.stereotype.Component;

/**
 * Twin of C# {@code PubSubBatchCompletionConsumerService}: a background subscriber that consumes
 * batch-completion messages and ingests a {@link BatchCompletionRecordEntity} into the reporting store.
 */
@Component
public class BatchCompletionConsumer implements SmartLifecycle {

    private static final Logger log = LoggerFactory.getLogger(BatchCompletionConsumer.class);

    private final PubSubConfig config;
    private final PubSubSupport pubSubSupport;
    private final BatchCompletionRecordRepository repository;
    private final ObjectMapper objectMapper;

    private Subscriber subscriber;
    private volatile boolean running;

    public BatchCompletionConsumer(PubSubConfig config, PubSubSupport pubSubSupport,
                                   BatchCompletionRecordRepository repository, ObjectMapper objectMapper) {
        this.config = config;
        this.pubSubSupport = pubSubSupport;
        this.repository = repository;
        this.objectMapper = objectMapper;
    }

    @Override
    public void start() {
        if (running || !config.isEnabled()) {
            return;
        }
        ProjectSubscriptionName subscriptionName = ProjectSubscriptionName.of(
                config.getProjectId(), config.getBatchCompletionSubscription());
        MessageReceiver receiver = this::onMessage;
        subscriber = Subscriber.newBuilder(subscriptionName, receiver)
                .setChannelProvider(pubSubSupport.channelProvider())
                .setCredentialsProvider(pubSubSupport.credentialsProvider())
                .build();
        subscriber.startAsync().awaitRunning();
        running = true;
        log.info("Batch completion Pub/Sub consumer started on subscription {}",
                config.getBatchCompletionSubscription());
    }

    private void onMessage(com.google.pubsub.v1.PubsubMessage message, AckReplyConsumer ack) {
        try {
            BatchCompletionMessage event = objectMapper.readValue(
                    message.getData().toStringUtf8(), BatchCompletionMessage.class);
            BatchCompletionRecordEntity record = new BatchCompletionRecordEntity();
            record.setBatchId(event.batchId());
            record.setRecipeType(event.recipeType());
            record.setCompletedAt(event.completedAt());
            repository.save(record);
            ack.ack();
        } catch (Exception e) {
            log.error("Failed to process batch completion message", e);
            ack.nack();
        }
    }

    @Override
    public void stop() {
        if (subscriber != null) {
            subscriber.stopAsync().awaitTerminated();
        }
        running = false;
    }

    @Override
    public boolean isRunning() {
        return running;
    }
}
