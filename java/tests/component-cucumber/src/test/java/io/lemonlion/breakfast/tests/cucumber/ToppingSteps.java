package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.ToppingRequest;
import io.lemonlion.breakfast.model.request.UpdateToppingRequest;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import java.util.List;
import java.util.UUID;

/** Cucumber step definitions for the Toppings domain. */
public class ToppingSteps {

    private static final UUID SEEDED = UUID.fromString("11111111-0000-0000-0000-000000000003");

    private final ScenarioContext ctx;

    public ToppingSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("the topping catalogue is requested")
    public void theToppingCatalogueIsRequested() {
        ctx.lastResponse = ctx.client().get("/toppings");
    }

    @When("a topping named {string} in category {string} is added")
    public void aToppingIsAdded(String name, String category) {
        ctx.lastResponse = ctx.client().post("/toppings", new ToppingRequest(name, category));
    }

    @Then("the catalogue includes {string}")
    public void theCatalogueIncludes(String name) {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        List<ToppingResponse> toppings = ctx.lastResponse.as(new TypeReference<List<ToppingResponse>>() { });
        assertThat(toppings).extracting(ToppingResponse::name).contains(name);
    }

    @Then("the created topping has an id")
    public void theCreatedToppingHasAnId() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(ctx.lastResponse.as(ToppingResponse.class).toppingId()).isNotNull();
    }

    @When("an existing topping is updated")
    public void anExistingToppingIsUpdated() {
        ctx.lastResponse = ctx.client().put("/toppings/" + SEEDED, new UpdateToppingRequest("Golden Syrup", "Syrup"));
    }

    @Then("the updated topping is named {string}")
    public void theUpdatedToppingIsNamed(String name) {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.as(ToppingResponse.class).name()).isEqualTo(name);
    }

    @When("a non-existent topping is updated")
    public void aNonExistentToppingIsUpdated() {
        ctx.lastResponse = ctx.client().put("/toppings/" + UUID.randomUUID(), new UpdateToppingRequest("X", "Y"));
    }

    @When("an existing topping is deleted")
    public void anExistingToppingIsDeleted() {
        ctx.lastResponse = ctx.client().delete("/toppings/" + SEEDED);
    }

    @When("a non-existent topping is deleted")
    public void aNonExistentToppingIsDeleted() {
        ctx.lastResponse = ctx.client().delete("/toppings/" + UUID.randomUUID());
    }

    @When("an existing topping is updated with HTML or script content")
    public void anExistingToppingIsUpdatedWithHtml() {
        ctx.lastResponse = ctx.client().put("/toppings/" + SEEDED,
                new UpdateToppingRequest("<script>alert(1)</script>", "Syrup"));
    }
}
