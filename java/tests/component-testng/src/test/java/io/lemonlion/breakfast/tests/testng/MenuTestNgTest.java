package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.response.MenuItemResponse;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import org.testng.annotations.Test;

/** Menu domain component tests (TestNG). */
public class MenuTestNgTest extends ComponentTestBaseNg {

    private static final TypeReference<List<MenuItemResponse>> MENU = new TypeReference<>() { };

    @Test
    public void menuAvailable() {
        BreakfastBackends.supplier().setAvailabilityStatus(200);
        client.delete("/menu/cache");

        TestResponse response = client.get("/menu");

        assertThat(response.status()).isEqualTo(200);
        List<MenuItemResponse> menu = response.as(MENU);
        assertThat(menu).extracting(MenuItemResponse::name).contains("Belgian Waffles");
        assertThat(menu).allMatch(MenuItemResponse::isAvailable);
    }

    @Test
    public void menuUnavailableWhenSupplierDown() {
        BreakfastBackends.supplier().setAvailabilityStatus(503);
        client.delete("/menu/cache");

        TestResponse response = client.get("/menu");

        assertThat(response.status()).isEqualTo(200);
        assertThat(response.as(MENU)).noneMatch(MenuItemResponse::isAvailable);
    }

    @Test
    public void clearCache() {
        assertThat(client.delete("/menu/cache").status()).isEqualTo(204);
    }
}
