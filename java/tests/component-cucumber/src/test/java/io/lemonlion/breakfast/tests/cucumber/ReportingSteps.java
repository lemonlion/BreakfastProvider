package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import java.util.List;
import java.util.Map;
import java.util.UUID;

/** Cucumber step definitions for the Reporting (GraphQL) domain. */
public class ReportingSteps {

    private final ScenarioContext ctx;
    private String customer;

    public ReportingSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("an order is placed and the order summaries are queried via GraphQL")
    public void anOrderIsPlacedAndQueried() {
        customer = "Cust-" + UUID.randomUUID();
        ctx.client().post("/orders",
                new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 4));
        ctx.lastResponse = ctx.client().post("/graphql",
                Map.of("query", "{ orderSummaries { orderId customerName } }"));
    }

    @Then("the order appears in the reporting summaries")
    public void theOrderAppearsInSummaries() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.bodyContains(customer)).isTrue();
    }

    @When("an order is placed and popular recipes are queried via GraphQL")
    public void anOrderIsPlacedAndPopularRecipesQueried() {
        ctx.client().post("/orders",
                new OrderRequest("Recipe-" + UUID.randomUUID(),
                        List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 4));
        ctx.lastResponse = ctx.client().post("/graphql",
                Map.of("query", "{ popularRecipes { recipeType count } }"));
    }

    @Then("the popular recipes include {string}")
    public void thePopularRecipesInclude(String recipe) {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(ctx.lastResponse.bodyContains(recipe)).isTrue();
    }
}
