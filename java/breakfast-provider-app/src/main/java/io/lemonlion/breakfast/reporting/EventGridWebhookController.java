package io.lemonlion.breakfast.reporting;

import com.fasterxml.jackson.databind.JsonNode;
import java.time.Instant;
import java.util.Map;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * Receives EventGrid events delivered via webhook (twin of the C# {@code EventGridWebhookController}).
 * In production Azure EventGrid pushes here; in component tests events are POSTed directly. Supports the
 * subscription-validation handshake and {@code IngredientDeliveryEvent} ingestion.
 */
@RestController
@RequestMapping("/webhooks/eventgrid")
public class EventGridWebhookController {

    private static final Logger log = LoggerFactory.getLogger(EventGridWebhookController.class);

    private final ReportingIngester ingester;

    public EventGridWebhookController(ReportingIngester ingester) {
        this.ingester = ingester;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> receiveEvents(@RequestBody JsonNode body) {
        Iterable<JsonNode> events = body.isArray() ? body : java.util.List.of(body);
        for (JsonNode event : events) {
            String eventType = event.path("eventType").asText(null);

            if ("Microsoft.EventGrid.SubscriptionValidationEvent".equals(eventType)) {
                String validationCode = event.path("data").path("validationCode").asText();
                return ResponseEntity.ok(Map.of("validationResponse", validationCode));
            }

            if (eventType != null && eventType.equalsIgnoreCase("IngredientDeliveryEvent")) {
                processIngredientDelivery(event.path("data"));
            } else {
                log.debug("Ignoring unhandled EventGrid event type: {}", eventType);
            }
        }
        return ResponseEntity.ok().build();
    }

    private void processIngredientDelivery(JsonNode data) {
        if (data.isMissingNode()) {
            return;
        }
        UUID deliveryId = UUID.fromString(data.path("deliveryId").asText());
        String ingredientName = data.path("ingredientName").asText();
        double quantity = data.path("quantity").asDouble();
        Instant deliveredAt = parseInstant(data.path("deliveredAt").asText());
        ingester.ingestIngredientShipment(deliveryId, ingredientName, quantity, deliveredAt);
        log.info("Processed ingredient delivery {} for {}", deliveryId, ingredientName);
    }

    private static Instant parseInstant(String value) {
        if (value == null || value.isBlank()) {
            return Instant.now();
        }
        try {
            return Instant.parse(value);
        } catch (RuntimeException ex) {
            return java.time.OffsetDateTime.parse(value).toInstant();
        }
    }
}
