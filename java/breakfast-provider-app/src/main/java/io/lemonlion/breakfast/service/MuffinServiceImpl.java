package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.events.EventHubPublisher;
import io.lemonlion.breakfast.events.PubSubPublisher;
import io.lemonlion.breakfast.events.RecipeLogger;
import io.lemonlion.breakfast.model.event.EquipmentAlertEvent;
import io.lemonlion.breakfast.model.event.MuffinBatchCompletedEvent;
import io.lemonlion.breakfast.model.event.RecipeLogEvent;
import io.lemonlion.breakfast.model.request.MuffinRequest;
import io.lemonlion.breakfast.model.request.MuffinTopping;
import io.lemonlion.breakfast.model.response.MuffinResponse;
import io.lemonlion.breakfast.reporting.BatchCompletionPublisher;
import java.time.Instant;
import java.util.List;
import java.util.UUID;
import org.springframework.stereotype.Service;

/**
 * Twin of C# {@code MuffinService}: assembles a batch (incl. apples/cinnamon + baking profile), logs the
 * recipe, and publishes batch-completed (Pub/Sub) and equipment-alert (Event Hubs) events.
 */
@Service
public class MuffinServiceImpl implements MuffinService {

    private final RecipeLogger recipeLogger;
    private final PubSubPublisher pubSubPublisher;
    private final EventHubPublisher eventHubPublisher;
    private final BatchCompletionPublisher batchCompletionPublisher;

    public MuffinServiceImpl(RecipeLogger recipeLogger, PubSubPublisher pubSubPublisher,
                             EventHubPublisher eventHubPublisher,
                             BatchCompletionPublisher batchCompletionPublisher) {
        this.recipeLogger = recipeLogger;
        this.pubSubPublisher = pubSubPublisher;
        this.eventHubPublisher = eventHubPublisher;
        this.batchCompletionPublisher = batchCompletionPublisher;
    }

    @Override
    public MuffinResponse makeMuffins(MuffinRequest request) {
        UUID batchId = UUID.randomUUID();
        Instant now = Instant.now();
        List<String> ingredients = List.of(
                request.milk(), request.flour(), request.eggs(), request.apples(), request.cinnamon());
        List<String> toppings = request.toppings() == null ? List.of()
                : request.toppings().stream().map(MuffinTopping::name).toList();

        recipeLogger.logRecipe(new RecipeLogEvent(batchId, "AppleCinnamonMuffins", ingredients, toppings, now));
        pubSubPublisher.publish(new MuffinBatchCompletedEvent(
                batchId, ingredients, toppings, request.baking().temperature(), now));
        eventHubPublisher.publish(new EquipmentAlertEvent(
                UUID.randomUUID(), batchId, "Muffin Oven", "UsageCycleCompleted", now));
        batchCompletionPublisher.publish("AppleCinnamonMuffins", batchId, now);

        return new MuffinResponse(batchId, ingredients, toppings,
                request.baking().temperature(), request.baking().durationMinutes(), now);
    }
}
