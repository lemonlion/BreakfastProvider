package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.WaffleRequest;
import io.lemonlion.breakfast.model.response.WaffleResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import org.testng.annotations.Test;

/** Waffles domain component tests (TestNG). */
public class WafflesTestNgTest extends ComponentTestBaseNg {

    @Test
    public void makesValidBatch() {
        WaffleRequest request = new WaffleRequest("Whole", "Plain", "Free-range", "Salted", List.of("Syrup"));
        TestResponse response = client.post("/waffles", request);

        assertThat(response.status()).isEqualTo(201);
        WaffleResponse batch = response.as(WaffleResponse.class);
        assertThat(batch.batchId()).isNotNull();
        assertThat(batch.ingredients()).containsExactly("Whole", "Plain", "Free-range", "Salted");
    }

    @Test
    public void rejectsMissingButter() {
        TestResponse response = client.post("/waffles",
                new WaffleRequest("Whole", "Plain", "Free-range", null, List.of()));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Butter' is required.")).isTrue();
    }
}
