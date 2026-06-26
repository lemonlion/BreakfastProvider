package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.DailySpecialOrderRequest;
import io.lemonlion.breakfast.model.response.DailySpecialOrderResponse;
import io.lemonlion.breakfast.model.response.DailySpecialResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.testng.annotations.Test;

/** DailySpecials domain component tests (TestNG). */
public class DailySpecialsTestNgTest extends ComponentTestBaseNg {

    private static final UUID SPECIAL = UUID.fromString("aaaa0000-0000-0000-0000-000000000001");
    private static final UUID LEMON_RICOTTA = UUID.fromString("aaaa0000-0000-0000-0000-000000000003");
    private static final int MAX_PER_SPECIAL = 100;

    @Test
    public void listsSpecials() {
        TestResponse response = client.get("/daily-specials");
        assertThat(response.status()).isEqualTo(200);
        List<DailySpecialResponse> specials = response.as(new TypeReference<List<DailySpecialResponse>>() { });
        assertThat(specials).extracting(DailySpecialResponse::name).contains("Matcha Waffles");
    }

    @Test
    public void orderIsIdempotent() {
        client.delete("/daily-specials/orders");
        String key = UUID.randomUUID().toString();
        DailySpecialOrderRequest request = new DailySpecialOrderRequest(SPECIAL, 1);
        DailySpecialOrderResponse first =
                client.post("/daily-specials/orders", request, Map.of("Idempotency-Key", key))
                        .as(DailySpecialOrderResponse.class);
        TestResponse repeat = client.post("/daily-specials/orders", request, Map.of("Idempotency-Key", key));
        assertThat(repeat.status()).isEqualTo(201);
        assertThat(repeat.as(DailySpecialOrderResponse.class).orderConfirmationId())
                .isEqualTo(first.orderConfirmationId());
    }

    @Test
    public void orderUnknownSpecial() {
        client.delete("/daily-specials/orders");
        assertThat(client.post("/daily-specials/orders", new DailySpecialOrderRequest(UUID.randomUUID(), 1)).status())
                .isEqualTo(404);
    }

    @Test
    public void soldOut() {
        client.delete("/daily-specials/orders");
        assertThat(client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 100)).status())
                .isEqualTo(201);
        assertThat(client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 1)).status())
                .isEqualTo(409);
    }

    @Test
    public void rejectsZeroQuantity() {
        TestResponse response = client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 0));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("Quantity must be greater than zero.")).isTrue();
    }

    @Test
    public void validOrderReturnsConfirmation() {
        client.delete("/daily-specials/orders");
        TestResponse response = client.post("/daily-specials/orders", new DailySpecialOrderRequest(SPECIAL, 1));
        assertThat(response.status()).isEqualTo(201);
        DailySpecialOrderResponse body = response.as(DailySpecialOrderResponse.class);
        assertThat(body.specialId()).isEqualTo(SPECIAL);
        assertThat(body.orderConfirmationId()).isNotNull();
    }

    @Test
    public void differentKeysReturnDifferentConfirmations() {
        client.delete("/daily-specials/orders");
        DailySpecialOrderRequest request = new DailySpecialOrderRequest(SPECIAL, 1);
        TestResponse first = client.post("/daily-specials/orders", request,
                Map.of("Idempotency-Key", UUID.randomUUID().toString()));
        TestResponse second = client.post("/daily-specials/orders", request,
                Map.of("Idempotency-Key", UUID.randomUUID().toString()));
        assertThat(first.status()).isEqualTo(201);
        assertThat(second.status()).isEqualTo(201);
        assertThat(second.as(DailySpecialOrderResponse.class).orderConfirmationId())
                .isNotEqualTo(first.as(DailySpecialOrderResponse.class).orderConfirmationId());
    }

    @Test
    public void remainingQuantityDecreases() {
        client.delete("/daily-specials/orders");
        assertThat(client.post("/daily-specials/orders", new DailySpecialOrderRequest(LEMON_RICOTTA, 1)).status())
                .isEqualTo(201);
        List<DailySpecialResponse> specials = client.get("/daily-specials")
                .as(new TypeReference<List<DailySpecialResponse>>() { });
        DailySpecialResponse lemonRicotta = specials.stream()
                .filter(s -> s.specialId().equals(LEMON_RICOTTA)).findFirst().orElseThrow();
        assertThat(lemonRicotta.remainingQuantity()).isEqualTo(MAX_PER_SPECIAL - 1);
    }
}
