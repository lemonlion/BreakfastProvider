package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.IngredientUsageRequest;
import io.lemonlion.breakfast.model.response.IngredientUsageResponse;
import io.lemonlion.breakfast.model.response.IngredientUsageSummaryResponse;
import java.util.List;

/** Twin of C# {@code IIngredientUsageService} (BigQuery-backed analytics). */
public interface IngredientUsageService {

    IngredientUsageResponse record(IngredientUsageRequest request);

    List<IngredientUsageSummaryResponse> getSummary();

    List<IngredientUsageResponse> listByIngredient(String ingredientName);
}
