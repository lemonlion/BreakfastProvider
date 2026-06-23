package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.BakingProfile;
import io.lemonlion.breakfast.model.request.MuffinRequest;
import io.lemonlion.breakfast.model.request.MuffinTopping;
import io.lemonlion.breakfast.model.response.MuffinResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Apple-cinnamon Muffins domain component tests (JUnit 5). */
@DisplayName("Muffins")
class MuffinsComponentTest extends ComponentTestBase {

    private static MuffinRequest validMuffins() {
        return new MuffinRequest("Whole", "Plain", "Free-range", "Bramley", "Ceylon",
                new BakingProfile(180, 25, "Silicone"), List.of(new MuffinTopping("Streusel", "2 tbsp")));
    }

    @Test
    @DisplayName("a valid muffin batch bakes with the requested profile")
    void makesValidBatch() {
        TestResponse response = client.post("/muffins", validMuffins());

        assertThat(response.status()).isEqualTo(201);
        MuffinResponse batch = response.as(MuffinResponse.class);
        assertThat(batch.batchId()).isNotNull();
        assertThat(batch.ingredients()).containsExactly("Whole", "Plain", "Free-range", "Bramley", "Ceylon");
        assertThat(batch.toppings()).containsExactly("Streusel");
        assertThat(batch.bakingTemperature()).isEqualTo(180);
        assertThat(batch.bakingDuration()).isEqualTo(25);
    }

    @Test
    @DisplayName("a muffin request without apples is rejected")
    void rejectsMissingApples() {
        MuffinRequest request = new MuffinRequest("Whole", "Plain", "Free-range", null, "Ceylon",
                new BakingProfile(180, 25, "Silicone"), List.of());
        TestResponse response = client.post("/muffins", request);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Apples' is required.")).isTrue();
    }

    @Test
    @DisplayName("a baking temperature outside 150-220 is rejected")
    void rejectsBadBakingTemperature() {
        MuffinRequest request = new MuffinRequest("Whole", "Plain", "Free-range", "Bramley", "Ceylon",
                new BakingProfile(300, 25, "Silicone"), List.of());
        TestResponse response = client.post("/muffins", request);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("Baking temperature must be between 150 and 220 degrees.")).isTrue();
    }
}
