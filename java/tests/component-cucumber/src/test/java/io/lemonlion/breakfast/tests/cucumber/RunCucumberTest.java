package io.lemonlion.breakfast.tests.cucumber;

import static io.cucumber.junit.platform.engine.Constants.GLUE_PROPERTY_NAME;
import static io.cucumber.junit.platform.engine.Constants.PLUGIN_PROPERTY_NAME;

import org.junit.platform.suite.api.ConfigurationParameter;
import org.junit.platform.suite.api.IncludeEngines;
import org.junit.platform.suite.api.SelectClasspathResource;
import org.junit.platform.suite.api.Suite;

/**
 * JUnit Platform suite that runs the Cucumber features. Registers the Kronikol4J Cucumber plugin so the
 * run produces a tracked Kronikol4J report.
 */
@Suite
@IncludeEngines("cucumber")
@SelectClasspathResource("features")
@ConfigurationParameter(key = GLUE_PROPERTY_NAME, value = "io.lemonlion.breakfast.tests.cucumber")
@ConfigurationParameter(key = PLUGIN_PROPERTY_NAME, value = "io.kronikol.cucumber.KronikolCucumberPlugin")
public class RunCucumberTest {
}
