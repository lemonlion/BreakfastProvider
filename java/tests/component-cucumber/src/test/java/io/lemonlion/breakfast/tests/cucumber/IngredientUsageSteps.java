package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.IngredientUsageRequest;
import io.lemonlion.breakfast.model.response.IngredientUsageResponse;
import java.math.BigDecimal;
import java.time.Duration;
import java.util.List;
import java.util.UUID;
import org.awaitility.Awaitility;

/** Cucumber step definitions for the IngredientUsage domain. */
public class IngredientUsageSteps {

    private static final TypeReference<List<IngredientUsageResponse>> USAGES = new TypeReference<>() { };

    private final ScenarioContext ctx;
    private String listedIngredient;

    public IngredientUsageSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("ingredient usage of {string} is recorded")
    public void ingredientUsageIsRecorded(String ingredient) {
        ctx.lastResponse = ctx.client().post("/ingredient-usage",
                new IngredientUsageRequest(ingredient, new BigDecimal("2.5"), "kg", "Classic Pancakes"));
    }

    @Then("the usage record is created")
    public void theUsageRecordIsCreated() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(ctx.lastResponse.as(IngredientUsageResponse.class).usageId()).isNotBlank();
    }

    @When("ingredient usage is recorded and listed by ingredient")
    public void ingredientUsageIsRecordedAndListedByIngredient() {
        listedIngredient = "Flour-" + UUID.randomUUID();
        ctx.client().post("/ingredient-usage",
                new IngredientUsageRequest(listedIngredient, new BigDecimal("2.5"), "kg", "Classic Pancakes"));
    }

    @Then("the usage list for that ingredient contains the record")
    public void theUsageListForThatIngredientContainsTheRecord() {
        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted(() -> {
            var listed = ctx.client().get("/ingredient-usage/ingredient/" + listedIngredient);
            assertThat(listed.status()).isEqualTo(200);
            assertThat(listed.as(USAGES)).anyMatch(u -> u.ingredientName().equals(listedIngredient));
        });
    }

    @When("the ingredient usage summary is requested")
    public void theIngredientUsageSummaryIsRequested() {
        ctx.client().post("/ingredient-usage",
                new IngredientUsageRequest("Sugar-" + UUID.randomUUID(), new BigDecimal("1"), "kg", "Waffles"));
        ctx.lastResponse = ctx.client().get("/ingredient-usage/summary");
    }

    @When("ingredient usage with zero quantity is recorded")
    public void ingredientUsageWithZeroQuantityIsRecorded() {
        ctx.lastResponse = ctx.client().post("/ingredient-usage",
                new IngredientUsageRequest("Flour", BigDecimal.ZERO, "kg", "Pancakes"));
    }

    @When("ingredient usage with a missing ingredient name is recorded")
    public void ingredientUsageWithMissingIngredientNameIsRecorded() {
        ctx.lastResponse = ctx.client().post("/ingredient-usage",
                new IngredientUsageRequest(null, new BigDecimal("2.5"), "kg", "Pancakes"));
    }
}
