package io.lemonlion.breakfast.tests.ratelimit;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.TestResponse;
import org.springframework.core.env.Environment;

/** Cucumber glue for the goat-milk feature-flag scenario in the isolated override context. */
public class GoatMilkFlagSteps {

    private final Environment environment;
    private BreakfastTestClient client;
    private TestResponse response;

    public GoatMilkFlagSteps(Environment environment) {
        this.environment = environment;
    }

    private BreakfastTestClient client() {
        if (client == null) {
            client = new BreakfastTestClient("http://127.0.0.1:" + environment.getProperty("local.server.port"));
        }
        return client;
    }

    @When("goat milk is requested in the override context")
    public void goatMilkRequested() {
        response = client().get("/goat-milk");
    }

    @Then("the override response status is {int}")
    public void overrideResponseStatusIs(int status) {
        assertThat(response.status()).isEqualTo(status);
    }
}
