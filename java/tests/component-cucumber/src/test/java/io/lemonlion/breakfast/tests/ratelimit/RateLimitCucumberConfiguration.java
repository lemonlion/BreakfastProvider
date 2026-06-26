package io.lemonlion.breakfast.tests.ratelimit;

import io.cucumber.spring.CucumberContextConfiguration;
import io.lemonlion.breakfast.BreakfastProviderApplication;
import io.lemonlion.breakfast.testsupport.BackendsInitializer;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.SpringBootTest.WebEnvironment;
import org.springframework.test.context.ContextConfiguration;
import org.springframework.test.context.TestPropertySource;

/**
 * Isolated Cucumber context for scenarios that need configuration overrides (cucumber-spring shares a
 * single context per glue set). Hosts the rate-limiting scenario ({@code permit-limit=1}) and the
 * toppings feature-flag scenario ({@code raspberry-topping-enabled=false}); these overrides don't
 * interfere (the flag scenario doesn't create orders). A unique in-process gRPC name avoids colliding
 * with the main Cucumber context that runs in the same JVM.
 */
@CucumberContextConfiguration
@SpringBootTest(classes = BreakfastProviderApplication.class, webEnvironment = WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer.class)
@TestPropertySource(properties = {
        "rate-limit.permit-limit=1",
        "rate-limit.window-seconds=60",
        "feature-switches.raspberry-topping-enabled=false",
        "feature-switches.goat-milk-enabled=false",
        "grpc.server.in-process-name=breakfast-grpc-ratelimit-cuke"})
public class RateLimitCucumberConfiguration {
}
