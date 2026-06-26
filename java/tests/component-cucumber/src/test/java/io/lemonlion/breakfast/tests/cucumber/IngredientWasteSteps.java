package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.IngredientWasteRequest;
import io.lemonlion.breakfast.model.response.IngredientWasteResponse;
import java.math.BigDecimal;
import java.time.Duration;
import java.util.List;
import java.util.UUID;
import org.awaitility.Awaitility;

/** Cucumber step definitions for the IngredientWaste domain. */
public class IngredientWasteSteps {

    private static final TypeReference<List<IngredientWasteResponse>> WASTES = new TypeReference<>() { };

    private final ScenarioContext ctx;
    private String listedRecipe;

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

    @When("ingredient waste is recorded and listed by recipe")
    public void ingredientWasteIsRecordedAndListedByRecipe() {
        listedRecipe = "Pancakes-" + UUID.randomUUID();
        ctx.client().post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", listedRecipe, "Burnt batch"));
    }

    @Then("the waste list for that recipe contains the record")
    public void theWasteListForThatRecipeContainsTheRecord() {
        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted(() -> {
            var listed = ctx.client().get("/ingredient-waste/recipe/" + listedRecipe);
            assertThat(listed.status()).isEqualTo(200);
            assertThat(listed.as(WASTES)).anyMatch(w -> w.recipeName().equals(listedRecipe));
        });
    }

    @When("a waste record is recorded and deleted")
    public void aWasteRecordIsRecordedAndDeleted() {
        IngredientWasteResponse waste = ctx.client().post("/ingredient-waste",
                        new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", "Spill"))
                .as(IngredientWasteResponse.class);
        ctx.lastResponse = ctx.client().delete("/ingredient-waste/" + waste.wasteId());
    }

    @When("ingredient waste with a missing ingredient name is recorded")
    public void ingredientWasteWithMissingIngredientNameIsRecorded() {
        ctx.lastResponse = ctx.client().post("/ingredient-waste",
                new IngredientWasteRequest(null, new BigDecimal("0.5"), "kg", "Pancakes", "Spill"));
    }

    @When("ingredient waste with zero quantity is recorded")
    public void ingredientWasteWithZeroQuantityIsRecorded() {
        ctx.lastResponse = ctx.client().post("/ingredient-waste",
                new IngredientWasteRequest("Flour", BigDecimal.ZERO, "kg", "Pancakes", "Spill"));
    }

    @When("ingredient waste with a missing reason is recorded")
    public void ingredientWasteWithMissingReasonIsRecorded() {
        ctx.lastResponse = ctx.client().post("/ingredient-waste",
                new IngredientWasteRequest("Flour", new BigDecimal("0.5"), "kg", "Pancakes", null));
    }
}
