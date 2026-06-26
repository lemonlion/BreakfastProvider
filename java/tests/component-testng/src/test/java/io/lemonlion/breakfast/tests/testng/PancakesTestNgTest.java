package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.PancakeRequest;
import io.lemonlion.breakfast.model.response.PancakeResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import org.testng.annotations.Test;

/** Pancakes domain component tests (TestNG). */
public class PancakesTestNgTest extends ComponentTestBaseNg {

    @Test
    public void makesValidBatch() {
        PancakeRequest request = new PancakeRequest("Whole", "Plain", "Free-range", List.of("Syrup", "Berries"));
        TestResponse response = client.post("/pancakes", request);

        assertThat(response.status()).isEqualTo(201);
        PancakeResponse batch = response.as(PancakeResponse.class);
        assertThat(batch.batchId()).isNotNull();
        assertThat(batch.ingredients()).containsExactly("Whole", "Plain", "Free-range");
    }

    @Test
    public void rejectsMissingMilk() {
        TestResponse response = client.post("/pancakes", new PancakeRequest(null, "Plain", "Free-range", List.of()));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Milk' is required.")).isTrue();
    }

    @Test
    public void rejectsTooManyToppings() {
        PancakeRequest request = new PancakeRequest("Whole", "Plain", "Free-range",
                List.of("a", "b", "c", "d", "e", "f"));
        TestResponse response = client.post("/pancakes", request);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("Maximum toppings exceeded. Limit is 5.")).isTrue();
    }

    @Test
    public void rejectsUnsupportedContentType() {
        TestResponse response = client.postRaw("/pancakes", "Whole Plain Free-range", "text/plain");
        assertThat(response.status()).isEqualTo(415);
    }
}
