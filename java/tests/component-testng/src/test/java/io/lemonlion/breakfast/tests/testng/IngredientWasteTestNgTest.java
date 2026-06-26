package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.IngredientWasteRequest;
import io.lemonlion.breakfast.model.response.IngredientWasteResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.math.BigDecimal;
import java.time.Duration;
import java.util.List;
import java.util.UUID;
import org.awaitility.Awaitility;
import org.testng.annotations.Test;

/** IngredientWaste domain component tests (TestNG). */
public class IngredientWasteTestNgTest extends ComponentTestBaseNg {

    private static final TypeReference<List<IngredientWasteResponse>> WASTES = new TypeReference<>() { };

    @Test
    public void recordReturnsCreated() {
        TestResponse recorded = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", "Burnt batch"));
        assertThat(recorded.status()).isEqualTo(201);
        assertThat(recorded.as(IngredientWasteResponse.class).wasteId()).isNotBlank();
    }

    @Test
    public void listByRecipe() {
        String recipe = "Pancakes-" + UUID.randomUUID();
        client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", recipe, "Burnt batch"));

        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted(() -> {
            TestResponse listed = client.get("/ingredient-waste/recipe/" + recipe);
            assertThat(listed.status()).isEqualTo(200);
            assertThat(listed.as(WASTES)).anyMatch(w -> w.recipeName().equals(recipe));
        });
    }

    @Test
    public void rejectsMissingReason() {
        TestResponse response = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", null));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Reason' must not be empty.")).isTrue();
    }

    @Test
    public void rejectsMissingIngredientName() {
        TestResponse response = client.post("/ingredient-waste",
                new IngredientWasteRequest(null, new BigDecimal("0.5"), "kg", "Pancakes", "Spill"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Ingredient Name' must not be empty.")).isTrue();
    }

    @Test
    public void rejectsZeroQuantity() {
        TestResponse response = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", BigDecimal.ZERO, "kg", "Pancakes", "Spill"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Quantity Wasted' must be greater than zero.")).isTrue();
    }

    @Test
    public void deleteReturns204() {
        IngredientWasteResponse waste = client.post("/ingredient-waste",
                        new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", "Spill"))
                .as(IngredientWasteResponse.class);
        assertThat(client.delete("/ingredient-waste/" + waste.wasteId()).status()).isEqualTo(204);
    }
}
