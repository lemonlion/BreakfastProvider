package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.BakingProfile;
import io.lemonlion.breakfast.model.request.MuffinRequest;
import io.lemonlion.breakfast.model.request.MuffinTopping;
import io.lemonlion.breakfast.model.response.MuffinResponse;
import java.util.List;

/** Cucumber step definitions for the Muffins domain. */
public class MuffinSteps {

    private final ScenarioContext ctx;
    private MuffinRequest request;

    public MuffinSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @Given("a valid muffin request")
    public void aValidMuffinRequest() {
        request = new MuffinRequest("Whole", "Plain", "Free-range", "Bramley", "Ceylon",
                new BakingProfile(180, 25, "Silicone"), List.of(new MuffinTopping("Streusel", "2 tbsp")));
    }

    @Given("a muffin request with an out-of-range baking temperature")
    public void aMuffinRequestWithBadTemperature() {
        request = new MuffinRequest("Whole", "Plain", "Free-range", "Bramley", "Ceylon",
                new BakingProfile(300, 25, "Silicone"), List.of());
    }

    @When("the muffins are made")
    public void theMuffinsAreMade() {
        ctx.lastResponse = ctx.client().post("/muffins", request);
    }

    @Then("a muffin batch is returned with the baking profile")
    public void aMuffinBatchIsReturned() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        MuffinResponse batch = ctx.lastResponse.as(MuffinResponse.class);
        assertThat(batch.batchId()).isNotNull();
        assertThat(batch.bakingTemperature()).isEqualTo(180);
    }
}
