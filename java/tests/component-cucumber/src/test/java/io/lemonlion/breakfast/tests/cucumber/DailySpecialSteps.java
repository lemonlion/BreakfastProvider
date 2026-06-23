package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.DailySpecialOrderRequest;
import io.lemonlion.breakfast.model.response.DailySpecialResponse;
import java.util.List;
import java.util.UUID;

/** Cucumber step definitions for the DailySpecials domain. */
public class DailySpecialSteps {

    private static final UUID SPECIAL = UUID.fromString("aaaa0000-0000-0000-0000-000000000001");

    private final ScenarioContext ctx;

    public DailySpecialSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("the daily specials are requested")
    public void theDailySpecialsAreRequested() {
        ctx.lastResponse = ctx.client().get("/daily-specials");
    }

    @When("a daily special is ordered")
    public void aDailySpecialIsOrdered() {
        ctx.client().delete("/daily-specials/orders");
        ctx.lastResponse = ctx.client().post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 1));
    }

    @Then("the specials list includes {string}")
    public void theSpecialsListIncludes(String name) {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        List<DailySpecialResponse> specials =
                ctx.lastResponse.as(new TypeReference<List<DailySpecialResponse>>() { });
        assertThat(specials).extracting(DailySpecialResponse::name).contains(name);
    }

    @Then("the daily special order is confirmed")
    public void theDailySpecialOrderIsConfirmed() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
    }
}
