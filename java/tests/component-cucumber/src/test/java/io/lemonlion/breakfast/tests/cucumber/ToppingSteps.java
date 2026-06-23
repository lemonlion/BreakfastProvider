package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.ToppingRequest;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import java.util.List;

/** Cucumber step definitions for the Toppings domain. */
public class ToppingSteps {

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
}
