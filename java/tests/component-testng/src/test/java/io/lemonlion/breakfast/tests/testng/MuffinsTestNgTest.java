package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.BakingProfile;
import io.lemonlion.breakfast.model.request.MuffinRequest;
import io.lemonlion.breakfast.model.request.MuffinTopping;
import io.lemonlion.breakfast.model.response.MuffinResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import org.testng.annotations.Test;

/** Apple-cinnamon Muffins domain component tests (TestNG). */
public class MuffinsTestNgTest extends ComponentTestBaseNg {

    private static MuffinRequest validMuffins() {
        return new MuffinRequest("Whole", "Plain", "Free-range", "Bramley", "Ceylon",
                new BakingProfile(180, 25, "Silicone"), List.of(new MuffinTopping("Streusel", "2 tbsp")));
    }

    @Test
    public void makesValidBatch() {
        TestResponse response = client.post("/muffins", validMuffins());
        assertThat(response.status()).isEqualTo(201);
        MuffinResponse batch = response.as(MuffinResponse.class);
        assertThat(batch.ingredients()).containsExactly("Whole", "Plain", "Free-range", "Bramley", "Ceylon");
        assertThat(batch.bakingTemperature()).isEqualTo(180);
    }

    @Test
    public void rejectsMissingApples() {
        MuffinRequest request = new MuffinRequest("Whole", "Plain", "Free-range", null, "Ceylon",
                new BakingProfile(180, 25, "Silicone"), List.of());
        TestResponse response = client.post("/muffins", request);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Apples' is required.")).isTrue();
    }

    @Test
    public void rejectsBadBakingTemperature() {
        MuffinRequest request = new MuffinRequest("Whole", "Plain", "Free-range", "Bramley", "Ceylon",
                new BakingProfile(300, 25, "Silicone"), List.of());
        TestResponse response = client.post("/muffins", request);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("Baking temperature must be between 150 and 220 degrees.")).isTrue();
    }
}
