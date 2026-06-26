package io.lemonlion.breakfast.reporting;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code IReportingIngester}: records order-created facts into the reporting store. */
public interface ReportingIngester {

    void ingestOrderCreated(UUID orderId, String customerName, int itemCount, Integer tableNumber, Instant createdAt,
                            List<String> recipeTypes);

    /** Records an ingredient delivery received via the EventGrid webhook (twin of C# IngredientShipment). */
    void ingestIngredientShipment(UUID deliveryId, String ingredientName, double quantity, Instant deliveredAt);

    /** Records a logged recipe (twin of C# {@code IngestRecipeLogAsync}); feeds recipeReports + ingredientUsage. */
    void ingestRecipeLog(UUID orderId, String recipeType, List<String> ingredients, List<String> toppings,
                         Instant loggedAt);
}
