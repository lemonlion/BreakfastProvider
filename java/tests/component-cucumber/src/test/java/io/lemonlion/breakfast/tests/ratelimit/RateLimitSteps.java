package io.lemonlion.breakfast.tests.ratelimit;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.springframework.core.env.Environment;

/** Cucumber glue for the isolated Orders rate-limiting context. */
public class RateLimitSteps {

    private final Environment environment;
    private BreakfastTestClient client;
    private TestResponse first;
    private TestResponse second;

    public RateLimitSteps(Environment environment) {
        this.environment = environment;
    }

    private BreakfastTestClient client() {
        if (client == null) {
            client = new BreakfastTestClient("http://127.0.0.1:" + environment.getProperty("local.server.port"));
            BreakfastBackends.resetFakes();
        }
        return client;
    }

    @When("two orders are placed within the rate-limit window")
    public void twoOrdersPlaced() {
        OrderRequest order = new OrderRequest(
                "RateLimit-" + UUID.randomUUID(),
                List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 1);
        first = client().post("/orders", order);
        second = client().post("/orders", order);
    }

    @Then("the first order succeeds and the second is rate limited")
    public void firstSucceedsSecondRateLimited() {
        assertThat(first.status()).isEqualTo(201);
        assertThat(second.status()).isEqualTo(429);
    }
}
