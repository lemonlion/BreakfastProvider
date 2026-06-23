package io.lemonlion.breakfast.tests.junit5;

import io.kronikol.junit5.KronikolExtension;
import io.lemonlion.breakfast.BreakfastProviderApplication;
import io.lemonlion.breakfast.testsupport.BackendsInitializer;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.extension.ExtendWith;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.SpringBootTest.WebEnvironment;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.test.context.ContextConfiguration;

/**
 * Base for JUnit 5 component tests: starts the SUT in-process on a random port against the shared
 * Testcontainers backends, and exposes a Kronikol4J-aware HTTP client. {@link KronikolExtension} scopes
 * each test's identity so the report attributes the SUT's interactions to the running test.
 */
@SpringBootTest(classes = BreakfastProviderApplication.class, webEnvironment = WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer.class)
@ExtendWith(KronikolExtension.class)
public abstract class ComponentTestBase {

    @LocalServerPort
    protected int port;

    protected BreakfastTestClient client;

    @BeforeEach
    void initClient() {
        client = new BreakfastTestClient("http://127.0.0.1:" + port);
        BreakfastBackends.kitchen().reset();
    }
}
