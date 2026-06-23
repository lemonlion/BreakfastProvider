package io.lemonlion.breakfast.tests.junit5;

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
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** IngredientUsage domain component tests (JUnit 5) — BigQuery analytics. */
@DisplayName("IngredientUsage")
class IngredientUsageComponentTest extends ComponentTestBase {

    private static final TypeReference<List<IngredientUsageResponse>> USAGES = new TypeReference<>() { };

    @Test
    @DisplayName("usage is recorded and queryable by ingredient")
    void recordAndQuery() {
        String ingredient = "Flour-" + UUID.randomUUID();
        TestResponse recorded = client.post("/ingredient-usage",
                new IngredientUsageRequest(ingredient, new BigDecimal("2.5"), "kg", "Classic Pancakes"));
        assertThat(recorded.status()).isEqualTo(201);
        assertThat(recorded.as(IngredientUsageResponse.class).usageId()).isNotBlank();

        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted(() -> {
            TestResponse listed = client.get("/ingredient-usage/ingredient/" + ingredient);
            assertThat(listed.status()).isEqualTo(200);
            assertThat(listed.as(USAGES)).anyMatch(u -> u.ingredientName().equals(ingredient));
        });
    }

    @Test
    @DisplayName("a non-positive quantity is rejected")
    void rejectsZeroQuantity() {
        TestResponse response = client.post("/ingredient-usage",
                new IngredientUsageRequest("Flour", BigDecimal.ZERO, "kg", "Pancakes"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Quantity Used' must be greater than zero.")).isTrue();
    }

    @Test
    @DisplayName("the usage summary is available")
    void summaryAvailable() {
        client.post("/ingredient-usage",
                new IngredientUsageRequest("Sugar-" + UUID.randomUUID(), new BigDecimal("1"), "kg", "Waffles"));
        TestResponse summary = client.get("/ingredient-usage/summary");
        assertThat(summary.status()).isEqualTo(200);
    }
}
