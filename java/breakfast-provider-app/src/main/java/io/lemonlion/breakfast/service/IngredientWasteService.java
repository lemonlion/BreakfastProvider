package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.IngredientWasteRequest;
import io.lemonlion.breakfast.model.response.IngredientWasteResponse;
import java.util.List;

/** Twin of C# {@code IIngredientWasteService} (BigQuery-backed). */
public interface IngredientWasteService {

    IngredientWasteResponse record(IngredientWasteRequest request);

    List<IngredientWasteResponse> listByRecipe(String recipeName);

    void delete(String wasteId);
}
