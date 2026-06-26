package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.request.PancakeRequest;
import io.lemonlion.breakfast.model.response.PancakeResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.time.Duration;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.awaitility.Awaitility;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Reporting domain component tests (JUnit 5) — GraphQL over the reporting store fed by order ingestion. */
@DisplayName("Reporting")
class ReportingComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("a created order appears in the GraphQL order summaries")
    void orderAppearsInSummaries() {
        String customer = "Cust-" + UUID.randomUUID();
        client.post("/orders",
                new OrderRequest(customer, List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 4));

        // Query path reads through a separate request/session; poll so a Cosmos cross-request
        // read-after-write lag under host load doesn't flake the single assert.
        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted(() -> {
            TestResponse gql = client.post("/graphql",
                    Map.of("query", "{ orderSummaries { orderId customerName itemCount } }"));
            assertThat(gql.status()).isEqualTo(200);
            assertThat(gql.bodyContains(customer)).isTrue();
        });
    }

    @Test
    @DisplayName("popular recipes reflects the ordered recipe types")
    void popularRecipesReflectsOrderedTypes() {
        client.post("/orders",
                new OrderRequest("Recipe-" + UUID.randomUUID(),
                        List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)), 4));

        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted(() -> {
            TestResponse gql = client.post("/graphql",
                    Map.of("query", "{ popularRecipes { recipeType count } }"));
            assertThat(gql.status()).isEqualTo(200);
            assertThat(gql.bodyContains("Pancakes")).isTrue();
        });
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

        Awaitility.await().atMost(Duration.ofSeconds(20)).untilAsserted(() -> {
            TestResponse gql = client.post("/graphql",
                    Map.of("query", "{ ingredientShipments { deliveryId ingredientName quantity } }"));
            assertThat(gql.status()).isEqualTo(200);
            assertThat(gql.bodyContains(deliveryId)).isTrue();
            assertThat(gql.bodyContains("Milk")).isTrue();
        });
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
    @DisplayName("a logged recipe is ingested into recipeReports via Kafka")
    void recipeReportIngestedViaKafka() {
        String marker = "Milk-" + UUID.randomUUID();
        client.post("/pancakes", new PancakeRequest(marker, "Plain", "Free-range", List.of("Syrup")));

        // 40s: the recipe-log Kafka consumer group can be slow to deliver the first message (cold start).
        Awaitility.await().atMost(Duration.ofSeconds(40)).untilAsserted(() -> {
            TestResponse gql = client.post("/graphql",
                    Map.of("query", "{ recipeReports { orderId recipeType ingredients toppings } }"));
            assertThat(gql.status()).isEqualTo(200);
            assertThat(gql.bodyContains("Pancakes")).isTrue();
            assertThat(gql.bodyContains(marker)).isTrue();
        });
    }

    @Test
    @DisplayName("ingredient usage aggregates across logged recipes")
    void ingredientUsageAggregatesAcrossRecipes() {
        String marker = "Flour-" + UUID.randomUUID();
        client.post("/pancakes", new PancakeRequest("Whole", marker, "Free-range", List.of("Syrup")));

        Awaitility.await().atMost(Duration.ofSeconds(40)).untilAsserted(() -> {
            TestResponse gql = client.post("/graphql",
                    Map.of("query", "{ ingredientUsage { ingredient count } }"));
            assertThat(gql.status()).isEqualTo(200);
            assertThat(gql.bodyContains(marker)).isTrue();
        });
    }

    @Test
    @DisplayName("a batch's equipment alert flows through Event Hubs into equipmentAlerts")
    void equipmentAlertFlowsThroughEventHub() {
        PancakeResponse batch = client.post("/pancakes",
                new PancakeRequest("Whole", "Plain", "Free-range", List.of("Syrup"))).as(PancakeResponse.class);
        String batchId = batch.batchId().toString();

        Awaitility.await().atMost(Duration.ofSeconds(40)).untilAsserted(() -> {
            TestResponse gql = client.post("/graphql",
                    Map.of("query", "{ equipmentAlerts { alertId batchId equipmentName alertType } }"));
            assertThat(gql.status()).isEqualTo(200);
            assertThat(gql.bodyContains(batchId)).isTrue();
        });
    }
}
