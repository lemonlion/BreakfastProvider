package io.lemonlion.breakfast.reporting;

import com.mongodb.client.MongoClient;
import io.lemonlion.breakfast.config.DownstreamConfig;
import io.lemonlion.breakfast.model.event.CustomerFeedbackReceivedEvent;
import io.lemonlion.breakfast.notification.NotificationClient;
import java.time.Instant;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;
import org.bson.Document;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.web.client.RestTemplateBuilder;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;

/**
 * Twin of C# {@code CustomerFeedbackAlertService}: on a consumed feedback event, store it in MongoDB
 * ({@code feedback_alerts}), send a gRPC notification, and POST it to the Supplier service.
 */
@Service
public class CustomerFeedbackAlertService {

    private static final Logger log = LoggerFactory.getLogger(CustomerFeedbackAlertService.class);

    private final MongoClient mongoClient;
    private final NotificationClient notificationClient;
    private final RestTemplate restTemplate;
    private final String supplierUrl;

    public CustomerFeedbackAlertService(MongoClient mongoClient, NotificationClient notificationClient,
                                        RestTemplateBuilder builder, DownstreamConfig downstreamConfig) {
        this.mongoClient = mongoClient;
        this.notificationClient = notificationClient;
        this.restTemplate = builder.build();
        this.supplierUrl = downstreamConfig.getSupplierServiceUrl();
    }

    public void processFeedback(CustomerFeedbackReceivedEvent feedback) {
        Document doc = new Document("_id", feedback.feedbackId().toString())
                .append("customerName", feedback.customerName())
                .append("recipeName", feedback.recipeName())
                .append("rating", feedback.rating())
                .append("comments", feedback.comments())
                .append("receivedAt", feedback.receivedAt() == null ? null : Date.from(feedback.receivedAt()))
                .append("processedAt", Date.from(Instant.now()));
        mongoClient.getDatabase("BreakfastDb").getCollection("feedback_alerts").insertOne(doc);

        notificationClient.sendOrderConfirmation(feedback.feedbackId(), feedback.customerName(), feedback.rating());

        Map<String, Object> body = new LinkedHashMap<>();
        body.put("feedbackId", feedback.feedbackId());
        body.put("recipeName", feedback.recipeName());
        body.put("rating", feedback.rating());
        body.put("customerName", feedback.customerName());
        restTemplate.postForEntity(supplierUrl + "/ingredients/feedback", body, Void.class);

        log.info("Processed customer feedback {} for recipe {}", feedback.feedbackId(), feedback.recipeName());
    }
}
