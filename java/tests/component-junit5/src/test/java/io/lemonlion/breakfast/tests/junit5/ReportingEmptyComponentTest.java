package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.JsonNode;
import io.kronikol.junit5.KronikolExtension;
import io.lemonlion.breakfast.BreakfastProviderApplication;
import io.lemonlion.breakfast.testsupport.BreakfastTestClient;
import io.lemonlion.breakfast.testsupport.EmptyReportingBackendsInitializer;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.Map;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.SpringBootTest.WebEnvironment;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.test.context.ContextConfiguration;

/**
 * Twin of the C# {@code Order_Summaries_Should_Return_An_Empty_List_When_No_Orders_Exist} scenario. Runs
 * in its own context whose relational store is a fresh empty H2 (via {@link EmptyReportingBackendsInitializer})
 * and which creates no orders, so the {@code orderSummaries} GraphQL query genuinely returns an empty list.
 */
@SpringBootTest(classes = BreakfastProviderApplication.class, webEnvironment = WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = EmptyReportingBackendsInitializer.class)
@ExtendWith(KronikolExtension.class)
@DisplayName("Reporting (empty store)")
class ReportingEmptyComponentTest {

    @LocalServerPort
    int port;

    @Test
    @DisplayName("order summaries return an empty list when no orders exist")
    void orderSummariesEmptyWhenNoOrders() {
        BreakfastTestClient client = new BreakfastTestClient("http://127.0.0.1:" + port);

        TestResponse gql = client.post("/graphql", Map.of("query", "{ orderSummaries { orderId } }"));

        assertThat(gql.status()).isEqualTo(200);
        JsonNode summaries = gql.json().get("data").get("orderSummaries");
        assertThat(summaries.isArray()).isTrue();
        assertThat(summaries.size()).isZero();
    }
}
