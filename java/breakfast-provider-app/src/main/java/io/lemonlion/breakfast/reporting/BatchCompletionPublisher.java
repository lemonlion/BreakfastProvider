package io.lemonlion.breakfast.reporting;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.google.cloud.pubsub.v1.Publisher;
import com.google.protobuf.ByteString;
import com.google.pubsub.v1.PubsubMessage;
import com.google.pubsub.v1.TopicName;
import io.lemonlion.breakfast.config.PubSubConfig;
import io.lemonlion.breakfast.persistence.pubsub.PubSubSupport;
import jakarta.annotation.PreDestroy;
import java.time.Instant;
import java.util.UUID;
import java.util.concurrent.TimeUnit;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

/**
 * Publishes recipe batch-completion events to Pub/Sub (twin of the C# batch-completion publisher), where
 * {@link BatchCompletionConsumer} ingests them into the reporting store. No-ops when Pub/Sub is disabled.
 */
@Component
public class BatchCompletionPublisher {

    private static final Logger log = LoggerFactory.getLogger(BatchCompletionPublisher.class);

    private final PubSubConfig config;
    private final PubSubSupport pubSubSupport;
    private final ObjectMapper objectMapper;

    private Publisher publisher;

    public BatchCompletionPublisher(PubSubConfig config, PubSubSupport pubSubSupport, ObjectMapper objectMapper) {
        this.config = config;
        this.pubSubSupport = pubSubSupport;
        this.objectMapper = objectMapper;
    }

    public void publish(String recipeType, UUID batchId, Instant completedAt) {
        if (!config.isEnabled()) {
            return;
        }
        try {
            String json = objectMapper.writeValueAsString(new BatchCompletionMessage(recipeType, batchId, completedAt));
            publisher().publish(PubsubMessage.newBuilder()
                    .setData(ByteString.copyFromUtf8(json)).build());
        } catch (Exception e) {
            log.warn("Failed to publish batch completion for {} batch {}", recipeType, batchId, e);
        }
    }

    private synchronized Publisher publisher() throws Exception {
        if (publisher == null) {
            publisher = Publisher.newBuilder(TopicName.of(config.getProjectId(), config.getBatchCompletionTopic()))
                    .setChannelProvider(pubSubSupport.channelProvider())
                    .setCredentialsProvider(pubSubSupport.credentialsProvider())
                    .build();
        }
        return publisher;
    }

    @PreDestroy
    public synchronized void shutdown() {
        if (publisher != null) {
            try {
                publisher.shutdown();
                publisher.awaitTermination(5, TimeUnit.SECONDS);
            } catch (Exception ignored) {
                // best-effort shutdown
            }
        }
    }
}
