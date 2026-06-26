package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.WaffleRequest;
import io.lemonlion.breakfast.model.response.WaffleResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Waffles domain component tests (JUnit 5). */
@DisplayName("Waffles")
class WafflesComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("a valid waffle batch is created with butter among the ingredients")
    void makesValidBatch() {
        WaffleRequest request = new WaffleRequest("Whole", "Plain", "Free-range", "Salted", List.of("Syrup"));

        TestResponse response = client.post("/waffles", request);

        assertThat(response.status()).isEqualTo(201);
        WaffleResponse batch = response.as(WaffleResponse.class);
        assertThat(batch.batchId()).isNotNull();
        assertThat(batch.ingredients()).containsExactly("Whole", "Plain", "Free-range", "Salted");
    }

    @Test
    @DisplayName("a waffle request without butter is rejected")
    void rejectsMissingButter() {
        TestResponse response = client.post("/waffles",
                new WaffleRequest("Whole", "Plain", "Free-range", null, List.of()));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Butter' is required.")).isTrue();
    }

    @Test
    @DisplayName("exceeding the topping limit is rejected")
    void rejectsTooManyToppings() {
        WaffleRequest request = new WaffleRequest("Whole", "Plain", "Free-range", "Salted",
                List.of("a", "b", "c", "d", "e", "f"));
        TestResponse response = client.post("/waffles", request);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("Maximum toppings exceeded. Limit is 5.")).isTrue();
    }

    @Test
    @DisplayName("an unsupported content type is rejected with 415")
    void rejectsUnsupportedContentType() {
        TestResponse response = client.postRaw("/waffles", "Whole Plain Free-range Salted", "text/plain");
        assertThat(response.status()).isEqualTo(415);
    }
}
