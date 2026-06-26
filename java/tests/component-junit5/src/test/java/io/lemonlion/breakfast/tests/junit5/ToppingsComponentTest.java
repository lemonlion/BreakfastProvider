package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.ToppingRequest;
import io.lemonlion.breakfast.model.request.UpdateToppingRequest;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Toppings domain component tests (JUnit 5). */
@DisplayName("Toppings")
class ToppingsComponentTest extends ComponentTestBase {

    private static final UUID SEEDED = UUID.fromString("11111111-0000-0000-0000-000000000003"); // Maple Syrup

    @Test
    @DisplayName("the topping catalogue is returned")
    void listsToppings() {
        TestResponse response = client.get("/toppings");
        assertThat(response.status()).isEqualTo(200);
        List<ToppingResponse> toppings = response.as(new TypeReference<List<ToppingResponse>>() { });
        assertThat(toppings).extracting(ToppingResponse::name).contains("Maple Syrup", "Blueberries");
    }

    @Test
    @DisplayName("a new topping is created and gets an id")
    void createsTopping() {
        TestResponse response = client.post("/toppings", new ToppingRequest("Caramel", "Syrup"));
        assertThat(response.status()).isEqualTo(201);
        ToppingResponse created = response.as(ToppingResponse.class);
        assertThat(created.toppingId()).isNotNull();
        assertThat(created.name()).isEqualTo("Caramel");
    }

    @Test
    @DisplayName("a topping without a name is rejected")
    void rejectsMissingName() {
        TestResponse response = client.post("/toppings", new ToppingRequest(null, "Syrup"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Name' is required.")).isTrue();
    }

    @Test
    @DisplayName("updating a seeded topping succeeds; a missing one is 404")
    void updateExistingAndMissing() {
        assertThat(client.put("/toppings/" + SEEDED, new UpdateToppingRequest("Golden Syrup", "Syrup")).status())
                .isEqualTo(200);
        assertThat(client.put("/toppings/" + UUID.randomUUID(), new UpdateToppingRequest("X", "Y")).status())
                .isEqualTo(404);
    }

    @Test
    @DisplayName("deleting a seeded topping is 204; a missing one is 404")
    void deleteExistingAndMissing() {
        assertThat(client.delete("/toppings/" + SEEDED).status()).isEqualTo(204);
        assertThat(client.delete("/toppings/" + UUID.randomUUID()).status()).isEqualTo(404);
    }

    @Test
    @DisplayName("raspberries are included when the feature flag is enabled")
    void raspberriesIncludedWhenEnabled() {
        List<ToppingResponse> toppings = client.get("/toppings")
                .as(new TypeReference<List<ToppingResponse>>() { });
        assertThat(toppings).extracting(ToppingResponse::name).contains("Raspberries");
    }

    @Test
    @DisplayName("a topping name with HTML/script content is rejected")
    void xssNameRejected() {
        TestResponse response = client.post("/toppings", new ToppingRequest("<script>alert(1)</script>", "Syrup"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("must not contain HTML or script content.")).isTrue();
    }
}
