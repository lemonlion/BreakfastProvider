package io.lemonlion.breakfast.reporting;

import com.fasterxml.jackson.databind.ObjectMapper;
import io.lemonlion.breakfast.model.event.RecipeCostCalculatedEvent;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.stereotype.Component;

/** Twin of C# {@code KafkaRecipeCostConsumerService}: consumes recipe-cost events from Kafka. */
@Component
public class RecipeCostConsumer {

    /** The Kafka topic carrying {@code RecipeCostCalculatedEvent} messages. */
    public static final String TOPIC = "recipe-cost-calculated";

    private static final Logger log = LoggerFactory.getLogger(RecipeCostConsumer.class);

    private final RecipeCostAnalysisService analysisService;
    private final ObjectMapper objectMapper;

    public RecipeCostConsumer(RecipeCostAnalysisService analysisService, ObjectMapper objectMapper) {
        this.analysisService = analysisService;
        this.objectMapper = objectMapper;
    }

    @KafkaListener(topics = TOPIC, groupId = "recipe-cost-analysis")
    public void onMessage(String json) {
        try {
            analysisService.processCostCalculation(
                    objectMapper.readValue(json, RecipeCostCalculatedEvent.class));
        } catch (Exception e) {
            log.error("Failed to process recipe cost message", e);
        }
    }
}
