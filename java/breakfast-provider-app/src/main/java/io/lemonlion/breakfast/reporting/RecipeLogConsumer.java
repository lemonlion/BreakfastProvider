package io.lemonlion.breakfast.reporting;

import com.fasterxml.jackson.databind.ObjectMapper;
import io.lemonlion.breakfast.events.kafka.KafkaRecipeLogger;
import io.lemonlion.breakfast.model.event.RecipeLogEvent;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.stereotype.Component;

/**
 * Twin of the C# recipe-log reporting consumer: consumes {@link RecipeLogEvent} messages from the
 * {@code recipe-logs} Kafka topic and projects them into the reporting store, feeding the
 * {@code recipeReports} and {@code ingredientUsage} GraphQL queries.
 */
@Component
public class RecipeLogConsumer {

    private static final Logger log = LoggerFactory.getLogger(RecipeLogConsumer.class);

    private final ReportingIngester ingester;
    private final ObjectMapper objectMapper;

    public RecipeLogConsumer(ReportingIngester ingester, ObjectMapper objectMapper) {
        this.ingester = ingester;
        this.objectMapper = objectMapper;
    }

    @KafkaListener(topics = KafkaRecipeLogger.TOPIC, groupId = "recipe-report-projection")
    public void onMessage(String json) {
        try {
            RecipeLogEvent event = objectMapper.readValue(json, RecipeLogEvent.class);
            ingester.ingestRecipeLog(
                    event.orderId(), event.recipeType(), event.ingredients(), event.toppings(), event.loggedAt());
            log.info("Ingested recipe report for order {} ({})", event.orderId(), event.recipeType());
        } catch (Exception e) {
            log.error("Failed to process recipe-log message", e);
        }
    }
}
