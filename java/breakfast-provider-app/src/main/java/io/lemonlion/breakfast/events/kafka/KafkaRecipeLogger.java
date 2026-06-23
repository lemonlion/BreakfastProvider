package io.lemonlion.breakfast.events.kafka;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.lemonlion.breakfast.events.RecipeLogger;
import io.lemonlion.breakfast.model.event.RecipeLogEvent;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Component;

/** Twin of the C# Kafka recipe logger: publishes {@link RecipeLogEvent} to the {@code recipe-logs} topic. */
@Component
public class KafkaRecipeLogger implements RecipeLogger {

    public static final String TOPIC = "recipe-logs";

    private final KafkaTemplate<String, String> kafkaTemplate;
    private final ObjectMapper objectMapper;

    public KafkaRecipeLogger(KafkaTemplate<String, String> kafkaTemplate, ObjectMapper objectMapper) {
        this.kafkaTemplate = kafkaTemplate;
        this.objectMapper = objectMapper;
    }

    @Override
    public void logRecipe(RecipeLogEvent recipe) {
        try {
            kafkaTemplate.send(TOPIC, recipe.orderId().toString(), objectMapper.writeValueAsString(recipe));
        } catch (JsonProcessingException e) {
            throw new IllegalStateException("Failed to serialize recipe log " + recipe.orderId(), e);
        }
    }
}
