package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.InventoryItemRequest;
import io.lemonlion.breakfast.model.response.InventoryItemResponse;
import java.math.BigDecimal;
import java.util.List;

/** Cucumber step definitions for the Inventory domain. */
public class InventorySteps {

    private final ScenarioContext ctx;
    private long createdId;

    public InventorySteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    private static InventoryItemRequest valid() {
        return new InventoryItemRequest("Flour", "Dry Goods", new BigDecimal("25.5"), "kg", new BigDecimal("5"));
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

    @When("an inventory item is added and retrieved by id")
    public void anInventoryItemIsAddedAndRetrievedById() {
        createdId = ctx.client().post("/inventory", valid()).as(InventoryItemResponse.class).id();
        ctx.lastResponse = ctx.client().get("/inventory/" + createdId);
    }

    @Then("the retrieved inventory item matches")
    public void theRetrievedInventoryItemMatches() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(InventoryItemResponse.class).id()).isEqualTo(createdId);
    }

    @When("an inventory item is added and all items are listed")
    public void anInventoryItemIsAddedAndAllItemsAreListed() {
        createdId = ctx.client().post("/inventory", valid()).as(InventoryItemResponse.class).id();
        ctx.lastResponse = ctx.client().get("/inventory");
    }

    @Then("the inventory list contains the item")
    public void theInventoryListContainsTheItem() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(new TypeReference<List<InventoryItemResponse>>() { }))
                .anyMatch(i -> i.id() == createdId);
    }

    @When("an inventory item is added and its quantity is updated")
    public void anInventoryItemIsAddedAndUpdated() {
        createdId = ctx.client().post("/inventory", valid()).as(InventoryItemResponse.class).id();
        ctx.lastResponse = ctx.client().put("/inventory/" + createdId,
                new InventoryItemRequest("Flour", "Dry Goods", new BigDecimal("10"), "kg", new BigDecimal("5")));
    }

    @When("an inventory item is added and deleted")
    public void anInventoryItemIsAddedAndDeleted() {
        createdId = ctx.client().post("/inventory", valid()).as(InventoryItemResponse.class).id();
        ctx.lastResponse = ctx.client().delete("/inventory/" + createdId);
    }

    @When("a non-existent inventory item is retrieved")
    public void aNonExistentInventoryItemIsRetrieved() {
        ctx.lastResponse = ctx.client().get("/inventory/999999999");
    }
}
