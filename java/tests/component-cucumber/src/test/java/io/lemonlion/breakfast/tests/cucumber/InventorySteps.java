package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.InventoryItemRequest;
import io.lemonlion.breakfast.model.response.InventoryItemResponse;
import java.math.BigDecimal;

/** Cucumber step definitions for the Inventory domain. */
public class InventorySteps {

    private final ScenarioContext ctx;

    public InventorySteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("an inventory item {string} with quantity {string} is added")
    public void anInventoryItemIsAdded(String name, String quantity) {
        ctx.lastResponse = ctx.client().post("/inventory",
                new InventoryItemRequest(name, "Dry Goods", new BigDecimal(quantity), "kg", BigDecimal.ZERO));
    }

    @Then("the inventory item is stored")
    public void theInventoryItemIsStored() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(ctx.lastResponse.as(InventoryItemResponse.class).id()).isPositive();
    }
}
