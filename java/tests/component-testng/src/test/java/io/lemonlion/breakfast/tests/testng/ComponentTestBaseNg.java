package io.lemonlion.breakfast.tests.testng;

import io.lemonlion.breakfast.BreakfastProviderApplication;
import io.lemonlion.breakfast.testsupport.BackendsInitializer;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.SpringBootTest.WebEnvironment;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.test.context.ContextConfiguration;
import org.springframework.test.context.testng.AbstractTestNGSpringContextTests;
import org.testng.annotations.BeforeMethod;

/**
 * Base for TestNG component tests. The Kronikol4J TestNG listener (ServiceLoader-registered) scopes each
 * test's identity and finalizes the report fragment; the SUT runs in-process against shared backends.
 */
@SpringBootTest(classes = BreakfastProviderApplication.class, webEnvironment = WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer.class)
public abstract class ComponentTestBaseNg extends AbstractTestNGSpringContextTests {

    @LocalServerPort
    protected int port;

    protected BreakfastTestClient client;

    @BeforeMethod
    public void initClient() {
        client = new BreakfastTestClient("http://127.0.0.1:" + port);
        BreakfastBackends.resetFakes();
    }
}
