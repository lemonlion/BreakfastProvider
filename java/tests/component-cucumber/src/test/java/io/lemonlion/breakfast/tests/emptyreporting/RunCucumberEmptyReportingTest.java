package io.lemonlion.breakfast.tests.emptyreporting;

import static io.cucumber.junit.platform.engine.Constants.GLUE_PROPERTY_NAME;
import static io.cucumber.junit.platform.engine.Constants.PLUGIN_PROPERTY_NAME;

import org.junit.platform.suite.api.ConfigurationParameter;
import org.junit.platform.suite.api.IncludeEngines;
import org.junit.platform.suite.api.SelectClasspathResource;
import org.junit.platform.suite.api.Suite;

/**
 * Separate JUnit Platform suite for the order-summaries-empty scenario, which needs an isolated empty
 * reporting store. Glue scoped to {@code io.lemonlion.breakfast.tests.emptyreporting} so cucumber-spring
 * sees exactly one {@code @CucumberContextConfiguration} for this suite.
 */
@Suite
@IncludeEngines("cucumber")
@SelectClasspathResource("features-emptyreporting")
@ConfigurationParameter(key = GLUE_PROPERTY_NAME, value = "io.lemonlion.breakfast.tests.emptyreporting")
@ConfigurationParameter(key = PLUGIN_PROPERTY_NAME, value = "io.kronikol.cucumber.KronikolCucumberPlugin")
public class RunCucumberEmptyReportingTest {
}
