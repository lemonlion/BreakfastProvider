package io.lemonlion.breakfast.tests.emptyreporting;

import io.cucumber.spring.CucumberContextConfiguration;
import io.lemonlion.breakfast.BreakfastProviderApplication;
import io.lemonlion.breakfast.testsupport.EmptyReportingBackendsInitializer;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.SpringBootTest.WebEnvironment;
import org.springframework.test.context.ContextConfiguration;

/**
 * Isolated Cucumber context for the order-summaries-empty scenario: its relational store is a fresh empty
 * H2 (via {@link EmptyReportingBackendsInitializer}) and it creates no orders. Scoped to its own glue
 * package so cucumber-spring sees exactly one {@code @CucumberContextConfiguration} per suite.
 */
@CucumberContextConfiguration
@SpringBootTest(classes = BreakfastProviderApplication.class, webEnvironment = WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = EmptyReportingBackendsInitializer.class)
public class EmptyReportingCucumberConfiguration {
}
