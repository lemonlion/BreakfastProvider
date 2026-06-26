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

    // A seeded topping (Maple Syrup). update/delete are stateless over the seed, so reusing it across
    // tests is safe — neither mutates the catalogue.
    private static final UUID SEEDED = UUID.fromString("11111111-0000-0000-0000-000000000003");

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
    @DisplayName("updating an existing topping returns the updated topping")
    void updateExisting() {
        TestResponse updated = client.put("/toppings/" + SEEDED, new UpdateToppingRequest("Golden Syrup", "Syrup"));
        assertThat(updated.status()).isEqualTo(200);
        assertThat(updated.as(ToppingResponse.class).name()).isEqualTo("Golden Syrup");
    }

    @Test
    @DisplayName("updating a non-existent topping returns 404")
    void updateMissing() {
        assertThat(client.put("/toppings/" + UUID.randomUUID(), new UpdateToppingRequest("X", "Y")).status())
                .isEqualTo(404);
    }

    @Test
    @DisplayName("deleting an existing topping returns 204")
    void deleteExisting() {
        assertThat(client.delete("/toppings/" + SEEDED).status()).isEqualTo(204);
    }

    @Test
    @DisplayName("deleting a non-existent topping returns 404")
    void deleteMissing() {
        assertThat(client.delete("/toppings/" + UUID.randomUUID()).status()).isEqualTo(404);
    }

    @Test
    @DisplayName("updating a topping with HTML/script content is rejected")
    void updateXssRejected() {
        TestResponse response = client.put("/toppings/" + SEEDED,
                new UpdateToppingRequest("<script>alert(1)</script>", "Syrup"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("must not contain HTML or script content.")).isTrue();
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
