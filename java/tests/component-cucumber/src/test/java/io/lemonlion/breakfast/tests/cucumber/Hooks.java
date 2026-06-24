package io.lemonlion.breakfast.tests.cucumber;

import io.cucumber.java.Before;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;

/**
 * Resets the in-JVM fakes before every scenario. {@link ScenarioContext} is a singleton bean (shared
 * across scenarios), so without this hook fake state (e.g. a forced 503 health status) would leak
 * between scenarios.
 */
public class Hooks {

    @Before
    public void resetFakes() {
        BreakfastBackends.resetFakes();
    }
}
