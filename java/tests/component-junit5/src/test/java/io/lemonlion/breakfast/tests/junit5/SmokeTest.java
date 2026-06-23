package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.kronikol.junit5.KronikolExtension;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;

/**
 * Phase 0 smoke test: proves the Maven reactor builds, the Kronikol4J JUnit 5 adapter is wired, and a
 * {@code TestRunReport.html} is produced by the merge step. Domain scenarios replace this in Phase 1+.
 */
@ExtendWith(KronikolExtension.class)
@DisplayName("Breakfast Provider — pipeline smoke")
class SmokeTest {

    @Test
    @DisplayName("the Java test pipeline is wired and green")
    void pipelineIsGreen() {
        assertThat("breakfast").startsWith("break");
    }
}
