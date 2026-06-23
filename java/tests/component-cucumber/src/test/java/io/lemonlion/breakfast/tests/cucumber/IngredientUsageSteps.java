package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.IngredientUsageRequest;
import io.lemonlion.breakfast.model.response.IngredientUsageResponse;
import java.math.BigDecimal;

/** Cucumber step definitions for the IngredientUsage domain. */
public class IngredientUsageSteps {

    private final ScenarioContext ctx;

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
}
