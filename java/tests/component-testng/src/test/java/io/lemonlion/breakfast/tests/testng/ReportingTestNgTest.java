package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.testng.annotations.Test;

/** Reporting domain component tests (TestNG) — GraphQL order summaries. */
public class ReportingTestNgTest extends ComponentTestBaseNg {

    @Test
    public void orderAppearsInSummaries() {
        String customer = "Cust-" + UUID.randomUUID();
        client.post("/orders",
                new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 4));

        TestResponse gql = client.post("/graphql",
                Map.of("query", "{ orderSummaries { orderId customerName itemCount } }"));

        assertThat(gql.status()).isEqualTo(200);
        assertThat(gql.bodyContains(customer)).isTrue();
    }

    @Test
    public void popularRecipesReflectsOrderedTypes() {
        client.post("/orders",
                new OrderRequest("Recipe-" + UUID.randomUUID(),
                        List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 4));

        TestResponse gql = client.post("/graphql",
                Map.of("query", "{ popularRecipes { recipeType count } }"));

        assertThat(gql.status()).isEqualTo(200);
        assertThat(gql.bodyContains("Pancakes")).isTrue();
    }

    @Test
    public void eventGridWebhookIngestsIngredientShipment() {
        String deliveryId = UUID.randomUUID().toString();
        Map<String, Object> event = Map.of(
                "id", UUID.randomUUID().toString(),
                "eventType", "IngredientDeliveryEvent",
                "subject", "supply-chain/deliveries",
                "data", Map.of(
                        "deliveryId", deliveryId,
                        "ingredientName", "Milk",
                        "quantity", 50.0,
                        "deliveredAt", java.time.Instant.now().toString()));

        TestResponse webhook = client.post("/webhooks/eventgrid", List.of(event));
        assertThat(webhook.status()).isEqualTo(200);

        TestResponse gql = client.post("/graphql",
                Map.of("query", "{ ingredientShipments { deliveryId ingredientName quantity } }"));

        assertThat(gql.status()).isEqualTo(200);
        assertThat(gql.bodyContains(deliveryId)).isTrue();
        assertThat(gql.bodyContains("Milk")).isTrue();
    }
}
