package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.event.EquipmentAlertEvent;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.request.PancakeRequest;
import io.lemonlion.breakfast.model.response.PancakeResponse;
import io.lemonlion.breakfast.reporting.EquipmentAlertConsumer;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.awaitility.Awaitility;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;

/** Reporting domain component tests (JUnit 5) — GraphQL over the reporting store fed by order ingestion. */
@DisplayName("Reporting")
class ReportingComponentTest extends ComponentTestBase {

    @Autowired
    EquipmentAlertConsumer equipmentAlertConsumer;

    @Test
    @DisplayName("a created order appears in the GraphQL order summaries")
    void orderAppearsInSummaries() {
        String customer = "Cust-" + UUID.randomUUID();
        client.post("/orders",
                new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 4));

        TestResponse gql = client.post("/graphql",
                Map.of("query", "{ orderSummaries { orderId customerName itemCount } }"));

        assertThat(gql.status()).isEqualTo(200);
        assertThat(gql.bodyContains(customer)).isTrue();
    }

    @Test
    @DisplayName("popular recipes reflects the ordered recipe types")
    void popularRecipesReflectsOrderedTypes() {
        client.post("/orders",
                new OrderRequest("Recipe-" + UUID.randomUUID(),
                        List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 4));

        TestResponse gql = client.post("/graphql",
                Map.of("query", "{ popularRecipes { recipeType count } }"));

        assertThat(gql.status()).isEqualTo(200);
        assertThat(gql.bodyContains("Pancakes")).isTrue();
    }

    @Test
    @DisplayName("an ingredient delivery posted to the EventGrid webhook appears in ingredient shipments")
    void eventGridWebhookIngestsIngredientShipment() {
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

    @Test
    @DisplayName("a completed pancake batch is ingested into batch completions via Pub/Sub")
    void batchCompletionIngestedViaPubSub() {
        PancakeResponse batch = client.post("/pancakes",
                new PancakeRequest("Whole", "Plain", "Free-range", List.of("Syrup"))).as(PancakeResponse.class);
        String batchId = batch.batchId().toString();

        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted(() -> {
            TestResponse gql = client.post("/graphql",
                    Map.of("query", "{ batchCompletions { batchId recipeType } }"));
            assertThat(gql.status()).isEqualTo(200);
            assertThat(gql.bodyContains(batchId)).isTrue();
        });
    }

    @Test
    @DisplayName("an equipment alert is ingested and surfaced via the GraphQL query")
    void equipmentAlertIngested() {
        UUID alertId = UUID.randomUUID();
        equipmentAlertConsumer.ingest(new EquipmentAlertEvent(
                alertId, UUID.randomUUID(), "Griddle", "UsageCycleCompleted", Instant.now()));

        TestResponse gql = client.post("/graphql",
                Map.of("query", "{ equipmentAlerts { alertId equipmentName alertType } }"));

        assertThat(gql.status()).isEqualTo(200);
        assertThat(gql.bodyContains(alertId.toString())).isTrue();
    }
}
