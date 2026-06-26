package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.IngredientUsageRequest;
import io.lemonlion.breakfast.model.response.IngredientUsageResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.math.BigDecimal;
import java.time.Duration;
import java.util.List;
import java.util.UUID;
import org.awaitility.Awaitility;
import org.testng.annotations.Test;

/** IngredientUsage domain component tests (TestNG). */
public class IngredientUsageTestNgTest extends ComponentTestBaseNg {

    private static final TypeReference<List<IngredientUsageResponse>> USAGES = new TypeReference<>() { };

    @Test
    public void recordReturnsCreated() {
        TestResponse recorded = client.post("/ingredient-usage",
                new IngredientUsageRequest("Flour", new BigDecimal("2.5"), "kg", "Classic Pancakes"));
        assertThat(recorded.status()).isEqualTo(201);
        assertThat(recorded.as(IngredientUsageResponse.class).usageId()).isNotBlank();
    }

    @Test
    public void listByIngredient() {
        String ingredient = "Flour-" + UUID.randomUUID();
        client.post("/ingredient-usage",
                new IngredientUsageRequest(ingredient, new BigDecimal("2.5"), "kg", "Classic Pancakes"));

        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted(() -> {
            TestResponse listed = client.get("/ingredient-usage/ingredient/" + ingredient);
            assertThat(listed.status()).isEqualTo(200);
            assertThat(listed.as(USAGES)).anyMatch(u -> u.ingredientName().equals(ingredient));
        });
    }

    @Test
    public void summaryAvailable() {
        client.post("/ingredient-usage",
                new IngredientUsageRequest("Sugar-" + UUID.randomUUID(), new BigDecimal("1"), "kg", "Waffles"));
        assertThat(client.get("/ingredient-usage/summary").status()).isEqualTo(200);
    }

    @Test
    public void rejectsZeroQuantity() {
        TestResponse response = client.post("/ingredient-usage",
                new IngredientUsageRequest("Flour", BigDecimal.ZERO, "kg", "Pancakes"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Quantity Used' must be greater than zero.")).isTrue();
    }

    @Test
    public void rejectsMissingIngredientName() {
        TestResponse response = client.post("/ingredient-usage",
                new IngredientUsageRequest(null, new BigDecimal("2.5"), "kg", "Pancakes"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Ingredient Name' must not be empty.")).isTrue();
    }
}
