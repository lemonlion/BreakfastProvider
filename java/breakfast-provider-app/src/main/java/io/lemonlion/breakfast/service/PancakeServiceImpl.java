package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.events.EventHubPublisher;
import io.lemonlion.breakfast.events.PubSubPublisher;
import io.lemonlion.breakfast.events.RecipeLogger;
import io.lemonlion.breakfast.reporting.BatchCompletionPublisher;
import io.lemonlion.breakfast.model.event.EquipmentAlertEvent;
import io.lemonlion.breakfast.model.event.PancakeBatchCompletedEvent;
import io.lemonlion.breakfast.model.event.RecipeLogEvent;
import io.lemonlion.breakfast.model.request.PancakeRequest;
import io.lemonlion.breakfast.model.response.PancakeResponse;
import java.time.Instant;
import java.util.List;
import java.util.UUID;
import org.springframework.stereotype.Service;

/**
 * Twin of C# {@code PancakeService}: assembles a batch from the ingredients, logs the recipe to Kafka,
 * and publishes the batch-completed (Pub/Sub) and equipment-alert (Event Hubs) events.
 */
@Service
public class PancakeServiceImpl implements PancakeService {

    private final RecipeLogger recipeLogger;
    private final PubSubPublisher pubSubPublisher;
    private final EventHubPublisher eventHubPublisher;
    private final BatchCompletionPublisher batchCompletionPublisher;

    public PancakeServiceImpl(RecipeLogger recipeLogger, PubSubPublisher pubSubPublisher,
                              EventHubPublisher eventHubPublisher,
                              BatchCompletionPublisher batchCompletionPublisher) {
        this.recipeLogger = recipeLogger;
        this.pubSubPublisher = pubSubPublisher;
        this.eventHubPublisher = eventHubPublisher;
        this.batchCompletionPublisher = batchCompletionPublisher;
    }

    @Override
    public PancakeResponse makePancakes(PancakeRequest request) {
        UUID batchId = UUID.randomUUID();
        Instant now = Instant.now();
        List<String> ingredients = List.of(request.milk(), request.flour(), request.eggs());

        recipeLogger.logRecipe(new RecipeLogEvent(batchId, "Pancakes", ingredients, request.toppings(), now));
        pubSubPublisher.publish(new PancakeBatchCompletedEvent(batchId, ingredients, request.toppings(), now));
        eventHubPublisher.publish(new EquipmentAlertEvent(
                UUID.randomUUID(), batchId, "Griddle", "UsageCycleCompleted", now));
        batchCompletionPublisher.publish("Pancakes", batchId, now);

        return new PancakeResponse(batchId, ingredients, request.toppings(), now);
    }
}
