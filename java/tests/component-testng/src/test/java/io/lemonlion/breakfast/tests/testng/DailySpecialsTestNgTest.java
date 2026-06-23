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
}
