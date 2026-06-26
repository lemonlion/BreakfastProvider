package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.PancakeRequest;
import io.lemonlion.breakfast.model.response.PancakeResponse;
import java.util.List;

/** Cucumber step definitions for the Pancakes domain. */
public class PancakeSteps {

    private final ScenarioContext ctx;
    private PancakeRequest request;

    public PancakeSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @Given("a valid pancake request")
    public void aValidPancakeRequest() {
        request = new PancakeRequest("Whole", "Plain", "Free-range", List.of("Syrup", "Berries"));
    }

    @Given("a pancake request without milk")
    public void aPancakeRequestWithoutMilk() {
        request = new PancakeRequest(null, "Plain", "Free-range", List.of());
    }

    @Given("a pancake request with six toppings")
    public void aPancakeRequestWithSixToppings() {
        request = new PancakeRequest("Whole", "Plain", "Free-range", List.of("a", "b", "c", "d", "e", "f"));
    }

    @When("the pancakes are made")
    public void thePancakesAreMade() {
        ctx.lastResponse = ctx.client().post("/pancakes", request);
    }

    @When("a pancake request is sent with an unsupported content type")
    public void aPancakeRequestWithUnsupportedContentType() {
        ctx.lastResponse = ctx.client().postRaw("/pancakes", "Whole Plain Free-range", "text/plain");
    }

    @Then("a pancake batch is returned with the ingredients")
    public void aPancakeBatchIsReturned() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        PancakeResponse batch = ctx.lastResponse.as(PancakeResponse.class);
        assertThat(batch.batchId()).isNotNull();
        assertThat(batch.ingredients()).containsExactly("Whole", "Plain", "Free-range");
    }
}
