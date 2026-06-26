package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.WaffleRequest;
import io.lemonlion.breakfast.model.response.WaffleResponse;
import java.util.List;

/** Cucumber step definitions for the Waffles domain. */
public class WaffleSteps {

    private final ScenarioContext ctx;
    private WaffleRequest request;

    public WaffleSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @Given("a valid waffle request")
    public void aValidWaffleRequest() {
        request = new WaffleRequest("Whole", "Plain", "Free-range", "Salted", List.of("Syrup"));
    }

    @Given("a waffle request without butter")
    public void aWaffleRequestWithoutButter() {
        request = new WaffleRequest("Whole", "Plain", "Free-range", null, List.of());
    }

    @Given("a waffle request with six toppings")
    public void aWaffleRequestWithSixToppings() {
        request = new WaffleRequest("Whole", "Plain", "Free-range", "Salted",
                List.of("a", "b", "c", "d", "e", "f"));
    }

    @When("the waffles are made")
    public void theWafflesAreMade() {
        ctx.lastResponse = ctx.client().post("/waffles", request);
    }

    @When("a waffle request is sent with an unsupported content type")
    public void aWaffleRequestWithUnsupportedContentType() {
        ctx.lastResponse = ctx.client().postRaw("/waffles", "Whole Plain Free-range Salted", "text/plain");
    }

    @Then("a waffle batch is returned with butter")
    public void aWaffleBatchIsReturned() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        WaffleResponse batch = ctx.lastResponse.as(WaffleResponse.class);
        assertThat(batch.batchId()).isNotNull();
        assertThat(batch.ingredients()).containsExactly("Whole", "Plain", "Free-range", "Salted");
    }
}
