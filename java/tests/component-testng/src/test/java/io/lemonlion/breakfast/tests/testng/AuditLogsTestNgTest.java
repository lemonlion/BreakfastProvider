package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.OrderItemRequest;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.AuditLogResponse;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.testng.annotations.Test;

/** AuditLogs domain component tests (TestNG). */
public class AuditLogsTestNgTest extends ComponentTestBaseNg {

    private static final TypeReference<List<AuditLogResponse>> LOGS = new TypeReference<>() { };

    @Test
    public void orderProducesAuditLog() {
        OrderResponse order = client.post("/orders",
                        new OrderRequest("Alice", List.of(new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)), 3))
                .as(OrderResponse.class);
        TestResponse logs = client.get("/audit-logs?entityId=" + order.orderId());
        assertThat(logs.status()).isEqualTo(200);
        assertThat(logs.as(LOGS)).anyMatch(l -> "Created".equals(l.action()) && "Order".equals(l.entityType()));
    }

    @Test
    public void filterByEntityType() {
        client.post("/orders",
                new OrderRequest("Bob", List.of(new OrderItemRequest("Waffles", UUID.randomUUID(), 1)), 4));
        TestResponse logs = client.get("/audit-logs?entityType=Order");
        assertThat(logs.status()).isEqualTo(200);
        assertThat(logs.as(LOGS)).isNotEmpty();
    }
}
