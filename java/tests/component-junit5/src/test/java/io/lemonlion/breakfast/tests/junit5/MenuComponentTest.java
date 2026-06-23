package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.response.MenuItemResponse;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Menu domain component tests (JUnit 5). */
@DisplayName("Menu")
class MenuComponentTest extends ComponentTestBase {

    private static final TypeReference<List<MenuItemResponse>> MENU = new TypeReference<>() { };

    @Test
    @DisplayName("the menu lists items as available when the supplier confirms ingredients")
    void menuAvailable() {
        BreakfastBackends.supplier().setAvailabilityStatus(200);
        client.delete("/menu/cache");

        TestResponse response = client.get("/menu");

        assertThat(response.status()).isEqualTo(200);
        List<MenuItemResponse> menu = response.as(MENU);
        assertThat(menu).extracting(MenuItemResponse::name).contains("Belgian Waffles", "Classic Pancakes");
        assertThat(menu).allMatch(MenuItemResponse::isAvailable);
    }

    @Test
    @DisplayName("when the supplier is down the menu items are marked unavailable")
    void menuUnavailableWhenSupplierDown() {
        BreakfastBackends.supplier().setAvailabilityStatus(503);
        client.delete("/menu/cache");

        TestResponse response = client.get("/menu");

        assertThat(response.status()).isEqualTo(200);
        assertThat(response.as(MENU)).noneMatch(MenuItemResponse::isAvailable);
    }

    @Test
    @DisplayName("the menu cache can be cleared")
    void clearCache() {
        assertThat(client.delete("/menu/cache").status()).isEqualTo(204);
    }
}
