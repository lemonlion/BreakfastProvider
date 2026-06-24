package io.lemonlion.breakfast.tests.ratelimit;

import io.cucumber.spring.CucumberContextConfiguration;
import io.lemonlion.breakfast.BreakfastProviderApplication;
import io.lemonlion.breakfast.testsupport.BackendsInitializer;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.SpringBootTest.WebEnvironment;
import org.springframework.test.context.ContextConfiguration;
import org.springframework.test.context.TestPropertySource;

/**
 * Isolated Cucumber context for the Orders rate-limiting feature. cucumber-spring shares a single
 * context per glue set, so the rate-limit scenario (which needs {@code permit-limit=1}) lives in its
 * own glue package with its own context. A unique in-process gRPC name avoids colliding with the main
 * Cucumber context that runs in the same JVM.
 */
@CucumberContextConfiguration
@SpringBootTest(classes = BreakfastProviderApplication.class, webEnvironment = WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer.class)
@TestPropertySource(properties = {
        "rate-limit.permit-limit=1",
        "rate-limit.window-seconds=60",
        "grpc.server.in-process-name=breakfast-grpc-ratelimit-cuke"})
public class RateLimitCucumberConfiguration {
}
