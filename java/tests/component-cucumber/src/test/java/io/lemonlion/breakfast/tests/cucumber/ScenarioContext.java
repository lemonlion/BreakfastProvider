package io.lemonlion.breakfast.tests.cucumber;

import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.TestResponse;
import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

/**
 * Scenario-scoped shared state for Cucumber glue (one per scenario): the HTTP client and last response.
 *
 * <p>The random server port is resolved lazily from the {@link Environment} when the client is first
 * used (at step-execution time, after the web server has started) rather than via {@code @LocalServerPort}
 * field injection — this bean is component-scanned (it lives under the SUT's base package) and so is
 * instantiated during context refresh, before the {@code local.server.port} property is published.
 */
@Component
public class ScenarioContext {

    private final Environment environment;

    private BreakfastTestClient client;

    /** The most recent HTTP response, set by the domain step classes and asserted by {@link CommonSteps}. */
    public TestResponse lastResponse;

    public ScenarioContext(Environment environment) {
        this.environment = environment;
    }

    public BreakfastTestClient client() {
        if (client == null) {
            String port = environment.getProperty("local.server.port");
            client = new BreakfastTestClient("http://127.0.0.1:" + port);
            BreakfastBackends.kitchen().reset();
        }
        return client;
    }
}
