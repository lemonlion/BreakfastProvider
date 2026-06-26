package io.lemonlion.breakfast.tests.junit5;

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
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** IngredientWaste domain component tests (JUnit 5) — BigQuery analytics. */
@DisplayName("IngredientWaste")
class IngredientWasteComponentTest extends ComponentTestBase {

    private static final TypeReference<List<IngredientWasteResponse>> WASTES = new TypeReference<>() { };

    @Test
    @DisplayName("recording waste returns the created record")
    void recordReturnsCreated() {
        TestResponse recorded = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", "Burnt batch"));
        assertThat(recorded.status()).isEqualTo(201);
        assertThat(recorded.as(IngredientWasteResponse.class).wasteId()).isNotBlank();
    }

    @Test
    @DisplayName("waste is queryable by recipe")
    void listByRecipe() {
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
    @DisplayName("a missing reason is rejected")
    void rejectsMissingReason() {
        TestResponse response = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", null));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Reason' must not be empty.")).isTrue();
    }

    @Test
    @DisplayName("a missing ingredient name is rejected")
    void rejectsMissingIngredientName() {
        TestResponse response = client.post("/ingredient-waste",
                new IngredientWasteRequest(null, new BigDecimal("0.5"), "kg", "Pancakes", "Spill"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Ingredient Name' must not be empty.")).isTrue();
    }

    @Test
    @DisplayName("a zero quantity is rejected")
    void rejectsZeroQuantity() {
        TestResponse response = client.post("/ingredient-waste",
                new IngredientWasteRequest("Flour", BigDecimal.ZERO, "kg", "Pancakes", "Spill"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Quantity Wasted' must be greater than zero.")).isTrue();
    }

    @Test
    @DisplayName("deleting a waste record returns 204")
    void deleteReturns204() {
        IngredientWasteResponse waste = client.post("/ingredient-waste",
                        new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", "Spill"))
                .as(IngredientWasteResponse.class);
        assertThat(client.delete("/ingredient-waste/" + waste.wasteId()).status()).isEqualTo(204);
    }
}
