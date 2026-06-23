package io.lemonlion.breakfast.tests.cucumber;

import io.cucumber.spring.CucumberContextConfiguration;
import io.lemonlion.breakfast.BreakfastProviderApplication;
import io.lemonlion.breakfast.testsupport.BackendsInitializer;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.SpringBootTest.WebEnvironment;
import org.springframework.test.context.ContextConfiguration;

/** Binds Cucumber's glue to the SUT's Spring Boot context running against the shared Testcontainers backends. */
@CucumberContextConfiguration
@SpringBootTest(classes = BreakfastProviderApplication.class, webEnvironment = WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer.class)
public class CucumberSpringConfiguration {
}
