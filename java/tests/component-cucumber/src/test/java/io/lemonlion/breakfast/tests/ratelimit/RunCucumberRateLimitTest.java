package io.lemonlion.breakfast.tests.ratelimit;

import static io.cucumber.junit.platform.engine.Constants.GLUE_PROPERTY_NAME;
import static io.cucumber.junit.platform.engine.Constants.PLUGIN_PROPERTY_NAME;

import org.junit.platform.suite.api.ConfigurationParameter;
import org.junit.platform.suite.api.IncludeEngines;
import org.junit.platform.suite.api.SelectClasspathResource;
import org.junit.platform.suite.api.Suite;

/**
 * Separate JUnit Platform suite for the isolated Orders rate-limiting feature. Its glue is scoped to
 * {@code io.lemonlion.breakfast.tests.ratelimit} (a sibling of, not a string prefix of, the main
 * Cucumber glue) so cucumber-spring sees exactly one {@code @CucumberContextConfiguration} per suite.
 */
@Suite
@IncludeEngines("cucumber")
@SelectClasspathResource("features-ratelimit")
@ConfigurationParameter(key = GLUE_PROPERTY_NAME, value = "io.lemonlion.breakfast.tests.ratelimit")
@ConfigurationParameter(key = PLUGIN_PROPERTY_NAME, value = "io.kronikol.cucumber.KronikolCucumberPlugin")
public class RunCucumberRateLimitTest {
}
