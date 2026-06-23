package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.AuditLogResponse;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** AuditLogs domain component tests (JUnit 5) — Cosmos audit-log query. */
@DisplayName("AuditLogs")
class AuditLogsComponentTest extends ComponentTestBase {

    private static final TypeReference<List<AuditLogResponse>> LOGS = new TypeReference<>() { };

    @Test
    @DisplayName("creating an order produces a queryable audit-log entry")
    void orderProducesAuditLog() {
        OrderResponse order = client.post("/orders",
                        new OrderRequest("Alice", List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 3))
                .as(OrderResponse.class);

        TestResponse logs = client.get("/audit-logs?entityId=" + order.orderId());
        assertThat(logs.status()).isEqualTo(200);
        assertThat(logs.as(LOGS))
                .anyMatch(l -> "Created".equals(l.action()) && "Order".equals(l.entityType()));
    }

    @Test
    @DisplayName("audit logs can be filtered by entity type")
    void filterByEntityType() {
        client.post("/orders",
                new OrderRequest("Bob", List.of(new OrderItemRequest("Waffles", UUID.randomUUID(), 1)), 4));
        TestResponse logs = client.get("/audit-logs?entityType=Order");
        assertThat(logs.status()).isEqualTo(200);
        assertThat(logs.as(LOGS)).isNotEmpty();
    }
}
