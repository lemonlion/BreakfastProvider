package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.ToppingRequest;
import io.lemonlion.breakfast.model.request.UpdateToppingRequest;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.testng.annotations.Test;

/** Toppings domain component tests (TestNG). */
public class ToppingsTestNgTest extends ComponentTestBaseNg {

    private static final UUID SEEDED = UUID.fromString("11111111-0000-0000-0000-000000000003");

    @Test
    public void listsToppings() {
        TestResponse response = client.get("/toppings");
        assertThat(response.status()).isEqualTo(200);
        List<ToppingResponse> toppings = response.as(new TypeReference<List<ToppingResponse>>() { });
        assertThat(toppings).extracting(ToppingResponse::name).contains("Maple Syrup");
    }

    @Test
    public void createsTopping() {
        TestResponse response = client.post("/toppings", new ToppingRequest("Caramel", "Syrup"));
        assertThat(response.status()).isEqualTo(201);
        assertThat(response.as(ToppingResponse.class).toppingId()).isNotNull();
    }

    @Test
    public void rejectsMissingName() {
        TestResponse response = client.post("/toppings", new ToppingRequest(null, "Syrup"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Name' is required.")).isTrue();
    }

    @Test
    public void updateExistingAndMissing() {
        assertThat(client.put("/toppings/" + SEEDED, new UpdateToppingRequest("Golden Syrup", "Syrup")).status())
                .isEqualTo(200);
        assertThat(client.put("/toppings/" + UUID.randomUUID(), new UpdateToppingRequest("X", "Y")).status())
                .isEqualTo(404);
    }

    @Test
    public void deleteExistingAndMissing() {
        assertThat(client.delete("/toppings/" + SEEDED).status()).isEqualTo(204);
        assertThat(client.delete("/toppings/" + UUID.randomUUID()).status()).isEqualTo(404);
    }
}
