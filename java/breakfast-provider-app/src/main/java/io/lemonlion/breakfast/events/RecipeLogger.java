package io.lemonlion.breakfast.events;

import io.lemonlion.breakfast.model.event.RecipeLogEvent;

/** Twin of C# {@code IRecipeLogger}: publishes a recipe-log event (to Kafka). */
public interface RecipeLogger {

    void logRecipe(RecipeLogEvent recipe);
}
