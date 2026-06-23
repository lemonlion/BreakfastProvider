package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.IngredientWasteRequest;
import io.lemonlion.breakfast.model.response.IngredientWasteResponse;
import java.math.BigDecimal;

/** Cucumber step definitions for the IngredientWaste domain. */
public class IngredientWasteSteps {

    private final ScenarioContext ctx;

    public IngredientWasteSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("ingredient waste for recipe {string} is recorded")
    public void ingredientWasteIsRecorded(String recipe) {
        ctx.lastResponse = ctx.client().post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", recipe, "Burnt batch"));
    }

    @Then("the waste record is created")
    public void theWasteRecordIsCreated() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(ctx.lastResponse.as(IngredientWasteResponse.class).wasteId()).isNotBlank();
    }
}
