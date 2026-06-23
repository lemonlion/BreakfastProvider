package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.events.PubSubPublisher;
import io.lemonlion.breakfast.events.RecipeLogger;
import io.lemonlion.breakfast.model.event.RecipeLogEvent;
import io.lemonlion.breakfast.model.event.WaffleBatchCompletedEvent;
import io.lemonlion.breakfast.model.request.WaffleRequest;
import io.lemonlion.breakfast.model.response.WaffleResponse;
import java.time.Instant;
import java.util.List;
import java.util.UUID;
import org.springframework.stereotype.Service;

/** Twin of C# {@code WaffleService}: assembles a batch (incl. butter), logs the recipe, publishes batch-completed. */
@Service
public class WaffleServiceImpl implements WaffleService {

    private final RecipeLogger recipeLogger;
    private final PubSubPublisher pubSubPublisher;

    public WaffleServiceImpl(RecipeLogger recipeLogger, PubSubPublisher pubSubPublisher) {
        this.recipeLogger = recipeLogger;
        this.pubSubPublisher = pubSubPublisher;
    }

    @Override
    public WaffleResponse makeWaffles(WaffleRequest request) {
        UUID batchId = UUID.randomUUID();
        Instant now = Instant.now();
        List<String> ingredients = List.of(request.milk(), request.flour(), request.eggs(), request.butter());

        recipeLogger.logRecipe(new RecipeLogEvent(batchId, "Waffles", ingredients, request.toppings(), now));
        pubSubPublisher.publish(new WaffleBatchCompletedEvent(batchId, ingredients, request.toppings(), now));

        return new WaffleResponse(batchId, ingredients, request.toppings(), now);
    }
}
