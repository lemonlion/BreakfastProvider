package io.lemonlion.breakfast.tests.cucumber;

import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.TestResponse;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.stereotype.Component;

/** Scenario-scoped shared state for Cucumber glue (one per scenario): the HTTP client and last response. */
@Component
public class ScenarioContext {

    @LocalServerPort
    int port;

    private BreakfastTestClient client;

    /** The most recent HTTP response, set by the domain step classes and asserted by {@link CommonSteps}. */
    public TestResponse lastResponse;

    public BreakfastTestClient client() {
        if (client == null) {
            client = new BreakfastTestClient("http://127.0.0.1:" + port);
            BreakfastBackends.kitchen().reset();
        }
        return client;
    }
}
