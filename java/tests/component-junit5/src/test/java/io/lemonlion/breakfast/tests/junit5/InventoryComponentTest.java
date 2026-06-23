package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.InventoryItemRequest;
import io.lemonlion.breakfast.model.response.InventoryItemResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.math.BigDecimal;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Inventory domain component tests (JUnit 5) — relational persistence. */
@DisplayName("Inventory")
class InventoryComponentTest extends ComponentTestBase {

    private static InventoryItemRequest valid() {
        return new InventoryItemRequest("Flour", "Dry Goods", new BigDecimal("25.5"), "kg", new BigDecimal("5"));
    }

    @Test
    @DisplayName("an inventory item is created and retrievable")
    void createAndRetrieve() {
        TestResponse created = client.post("/inventory", valid());
        assertThat(created.status()).isEqualTo(201);
        InventoryItemResponse item = created.as(InventoryItemResponse.class);
        assertThat(item.id()).isPositive();
        assertThat(item.unit()).isEqualTo("kg");
        assertThat(client.get("/inventory/" + item.id()).status()).isEqualTo(200);
    }

    @Test
    @DisplayName("a negative quantity is rejected")
    void rejectsNegativeQuantity() {
        InventoryItemRequest bad = new InventoryItemRequest("Flour", "Dry Goods", new BigDecimal("-1"), "kg",
                BigDecimal.ZERO);
        TestResponse response = client.post("/inventory", bad);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Quantity' must be greater than or equal to zero.")).isTrue();
    }

    @Test
    @DisplayName("updating the quantity succeeds")
    void updateQuantity() {
        InventoryItemResponse item = client.post("/inventory", valid()).as(InventoryItemResponse.class);
        InventoryItemRequest update = new InventoryItemRequest("Flour", "Dry Goods", new BigDecimal("10"), "kg",
                new BigDecimal("5"));
        TestResponse response = client.put("/inventory/" + item.id(), update);
        assertThat(response.status()).isEqualTo(200);
        assertThat(response.as(InventoryItemResponse.class).quantity()).isEqualByComparingTo(new BigDecimal("10"));
    }
}
