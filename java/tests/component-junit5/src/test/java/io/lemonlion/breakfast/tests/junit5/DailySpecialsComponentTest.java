package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.DailySpecialOrderRequest;
import io.lemonlion.breakfast.model.response.DailySpecialOrderResponse;
import io.lemonlion.breakfast.model.response.DailySpecialResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** DailySpecials domain component tests (JUnit 5) — in-memory counters + Cosmos idempotency + Pub/Sub. */
@DisplayName("DailySpecials")
class DailySpecialsComponentTest extends ComponentTestBase {

    private static final UUID SPECIAL = UUID.fromString("aaaa0000-0000-0000-0000-000000000001");

    @Test
    @DisplayName("the available daily specials are listed")
    void listsSpecials() {
        TestResponse response = client.get("/daily-specials");
        assertThat(response.status()).isEqualTo(200);
        List<DailySpecialResponse> specials = response.as(new TypeReference<List<DailySpecialResponse>>() { });
        assertThat(specials).extracting(DailySpecialResponse::name).contains("Matcha Waffles");
    }

    @Test
    @DisplayName("ordering a special succeeds and is idempotent under the same key")
    void orderIsIdempotent() {
        client.delete("/daily-specials/orders");
        String key = UUID.randomUUID().toString();
        DailySpecialOrderRequest request = new DailySpecialOrderRequest(SPECIAL, 1);

        TestResponse first = client.post("/daily-specials/orders", request, Map.of("Idempotency-Key", key));
        assertThat(first.status()).isEqualTo(201);
        DailySpecialOrderResponse firstBody = first.as(DailySpecialOrderResponse.class);

        TestResponse repeat = client.post("/daily-specials/orders", request, Map.of("Idempotency-Key", key));
        assertThat(repeat.status()).isEqualTo(201);
        assertThat(repeat.as(DailySpecialOrderResponse.class).orderConfirmationId())
                .isEqualTo(firstBody.orderConfirmationId());
    }

    @Test
    @DisplayName("ordering an unknown special returns 404")
    void orderUnknownSpecial() {
        client.delete("/daily-specials/orders");
        TestResponse response = client.post("/daily-specials/orders",
                new DailySpecialOrderRequest(UUID.randomUUID(), 1));
        assertThat(response.status()).isEqualTo(404);
    }

    @Test
    @DisplayName("exceeding the daily limit returns 409 sold out")
    void soldOut() {
        client.delete("/daily-specials/orders");
        assertThat(client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 100)).status())
                .isEqualTo(201);
        assertThat(client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 1)).status())
                .isEqualTo(409);
    }

    @Test
    @DisplayName("a zero quantity is rejected")
    void rejectsZeroQuantity() {
        TestResponse response = client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 0));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("Quantity must be greater than zero.")).isTrue();
    }
}
