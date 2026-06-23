package io.lemonlion.breakfast.events.kafka;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.lemonlion.breakfast.events.RecipeLogPublisher;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Component;

/** Twin of the C# recipe-log Kafka publisher. Emits a recipe-log event to the {@code recipe-logs} topic. */
@Component
public class KafkaRecipeLogPublisher implements RecipeLogPublisher {

    public static final String TOPIC = "recipe-logs";

    private final KafkaTemplate<String, String> kafkaTemplate;
    private final ObjectMapper objectMapper;

    public KafkaRecipeLogPublisher(KafkaTemplate<String, String> kafkaTemplate, ObjectMapper objectMapper) {
        this.kafkaTemplate = kafkaTemplate;
        this.objectMapper = objectMapper;
    }

    @Override
    public void publishOrderRecipeLog(UUID orderId, String customerName, int itemCount) {
        Map<String, Object> payload = new LinkedHashMap<>();
        payload.put("orderId", orderId.toString());
        payload.put("customerName", customerName);
        payload.put("itemCount", itemCount);
        payload.put("eventType", "OrderRecipeLog");
        try {
            kafkaTemplate.send(TOPIC, orderId.toString(), objectMapper.writeValueAsString(payload));
        } catch (JsonProcessingException e) {
            throw new IllegalStateException("Failed to serialize recipe log for order " + orderId, e);
        }
    }
}
