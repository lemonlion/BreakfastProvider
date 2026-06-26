package io.lemonlion.breakfast.tests.ratelimit;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import org.springframework.core.env.Environment;

/** Cucumber glue for the toppings feature-flag scenario in the isolated override context. */
public class ToppingFlagSteps {

    private final Environment environment;
    private BreakfastTestClient client;
    private TestResponse response;

    public ToppingFlagSteps(Environment environment) {
        this.environment = environment;
    }

    private BreakfastTestClient client() {
        if (client == null) {
            client = new BreakfastTestClient("http://127.0.0.1:" + environment.getProperty("local.server.port"));
        }
        return client;
    }

    @When("the topping catalogue is requested in the override context")
    public void theToppingCatalogueIsRequested() {
        response = client().get("/toppings");
    }

    @Then("the catalogue excludes {string}")
    public void theCatalogueExcludes(String name) {
        assertThat(response.status()).isEqualTo(200);
        List<ToppingResponse> toppings = response.as(new TypeReference<List<ToppingResponse>>() { });
        assertThat(toppings).extracting(ToppingResponse::name).doesNotContain(name);
    }
}
