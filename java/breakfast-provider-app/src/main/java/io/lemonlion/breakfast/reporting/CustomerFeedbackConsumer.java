package io.lemonlion.breakfast.reporting;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.google.cloud.pubsub.v1.AckReplyConsumer;
import com.google.cloud.pubsub.v1.MessageReceiver;
import com.google.cloud.pubsub.v1.Subscriber;
import com.google.pubsub.v1.ProjectSubscriptionName;
import io.lemonlion.breakfast.config.PubSubConfig;
import io.lemonlion.breakfast.model.event.CustomerFeedbackReceivedEvent;
import io.lemonlion.breakfast.persistence.pubsub.PubSubSupport;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.SmartLifecycle;
import org.springframework.stereotype.Component;

/**
 * Twin of C# {@code PubSubCustomerFeedbackConsumerService}: a background subscriber that consumes
 * {@code CustomerFeedbackReceivedEvent} messages and hands them to {@link CustomerFeedbackAlertService}.
 */
@Component
@ConditionalOnProperty(name = "breakfast.background-consumers.enabled", matchIfMissing = true)
public class CustomerFeedbackConsumer implements SmartLifecycle {

    private static final Logger log = LoggerFactory.getLogger(CustomerFeedbackConsumer.class);

    private final PubSubConfig config;
    private final PubSubSupport pubSubSupport;
    private final CustomerFeedbackAlertService alertService;
    private final ObjectMapper objectMapper;

    private Subscriber subscriber;
    private volatile boolean running;

    public CustomerFeedbackConsumer(PubSubConfig config, PubSubSupport pubSubSupport,
                                    CustomerFeedbackAlertService alertService, ObjectMapper objectMapper) {
        this.config = config;
        this.pubSubSupport = pubSubSupport;
        this.alertService = alertService;
        this.objectMapper = objectMapper;
    }

    @Override
    public void start() {
        if (running || !config.isEnabled()) {
            return;
        }
        ProjectSubscriptionName subscriptionName = ProjectSubscriptionName.of(
                config.getProjectId(), config.getCustomerFeedbackSubscription());
        MessageReceiver receiver = this::onMessage;
        subscriber = Subscriber.newBuilder(subscriptionName, receiver)
                .setChannelProvider(pubSubSupport.channelProvider())
                .setCredentialsProvider(pubSubSupport.credentialsProvider())
                .build();
        subscriber.startAsync().awaitRunning();
        running = true;
        log.info("Customer feedback Pub/Sub consumer started on subscription {}",
                config.getCustomerFeedbackSubscription());
    }

    private void onMessage(com.google.pubsub.v1.PubsubMessage message, AckReplyConsumer ack) {
        try {
            CustomerFeedbackReceivedEvent event = objectMapper.readValue(
                    message.getData().toStringUtf8(), CustomerFeedbackReceivedEvent.class);
            alertService.processFeedback(event);
            ack.ack();
        } catch (Exception e) {
            log.error("Failed to process customer feedback message", e);
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
