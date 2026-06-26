package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.DailySpecialOrderRequest;
import io.lemonlion.breakfast.model.response.DailySpecialOrderResponse;
import io.lemonlion.breakfast.model.response.DailySpecialResponse;
import java.util.List;
import java.util.Map;
import java.util.UUID;

/** Cucumber step definitions for the DailySpecials domain. */
public class DailySpecialSteps {

    private static final UUID SPECIAL = UUID.fromString("aaaa0000-0000-0000-0000-000000000001");
    private static final UUID LEMON_RICOTTA = UUID.fromString("aaaa0000-0000-0000-0000-000000000003");
    private static final int MAX_PER_SPECIAL = 100;

    private final ScenarioContext ctx;
    private UUID firstConfirmationId;
    private UUID secondConfirmationId;

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

    @When("an unknown daily special is ordered")
    public void anUnknownDailySpecialIsOrdered() {
        ctx.client().delete("/daily-specials/orders");
        ctx.lastResponse = ctx.client().post("/daily-specials/orders",
                new DailySpecialOrderRequest(UUID.randomUUID(), 1));
    }

    @When("the daily special is ordered beyond its limit")
    public void theDailySpecialIsOrderedBeyondItsLimit() {
        ctx.client().delete("/daily-specials/orders");
        ctx.client().post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, MAX_PER_SPECIAL));
        ctx.lastResponse = ctx.client().post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 1));
    }

    @When("a daily special is ordered with zero quantity")
    public void aDailySpecialIsOrderedWithZeroQuantity() {
        ctx.lastResponse = ctx.client().post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 0));
    }

    @When("a daily special is ordered twice with the same idempotency key")
    public void aDailySpecialIsOrderedTwiceWithTheSameKey() {
        ctx.client().delete("/daily-specials/orders");
        String key = UUID.randomUUID().toString();
        DailySpecialOrderRequest request = new DailySpecialOrderRequest(SPECIAL, 1);
        firstConfirmationId = ctx.client().post("/daily-specials/orders", request, Map.of("Idempotency-Key", key))
                .as(DailySpecialOrderResponse.class).orderConfirmationId();
        secondConfirmationId = ctx.client().post("/daily-specials/orders", request, Map.of("Idempotency-Key", key))
                .as(DailySpecialOrderResponse.class).orderConfirmationId();
    }

    @When("a daily special is ordered twice with different idempotency keys")
    public void aDailySpecialIsOrderedTwiceWithDifferentKeys() {
        ctx.client().delete("/daily-specials/orders");
        DailySpecialOrderRequest request = new DailySpecialOrderRequest(SPECIAL, 1);
        firstConfirmationId = ctx.client()
                .post("/daily-specials/orders", request, Map.of("Idempotency-Key", UUID.randomUUID().toString()))
                .as(DailySpecialOrderResponse.class).orderConfirmationId();
        secondConfirmationId = ctx.client()
                .post("/daily-specials/orders", request, Map.of("Idempotency-Key", UUID.randomUUID().toString()))
                .as(DailySpecialOrderResponse.class).orderConfirmationId();
    }

    @Then("both confirmations are identical")
    public void bothConfirmationsAreIdentical() {
        assertThat(secondConfirmationId).isEqualTo(firstConfirmationId);
    }

    @Then("the two confirmations differ")
    public void theTwoConfirmationsDiffer() {
        assertThat(secondConfirmationId).isNotEqualTo(firstConfirmationId);
    }

    @When("the lemon ricotta special is ordered once")
    public void theLemonRicottaSpecialIsOrderedOnce() {
        ctx.client().delete("/daily-specials/orders");
        ctx.client().post("/daily-specials/orders", new DailySpecialOrderRequest(LEMON_RICOTTA, 1));
        ctx.lastResponse = ctx.client().get("/daily-specials");
    }

    @Then("the lemon ricotta special has one fewer remaining")
    public void theLemonRicottaSpecialHasOneFewerRemaining() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        List<DailySpecialResponse> specials =
                ctx.lastResponse.as(new TypeReference<List<DailySpecialResponse>>() { });
        DailySpecialResponse lemonRicotta = specials.stream()
                .filter(s -> s.specialId().equals(LEMON_RICOTTA)).findFirst().orElseThrow();
        assertThat(lemonRicotta.remainingQuantity()).isEqualTo(MAX_PER_SPECIAL - 1);
    }
}
