package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.response.MenuItemResponse;
import io.lemonlion.breakfast.testsupport.BreakfastBackends;
import java.util.List;

/** Cucumber step definitions for the Menu domain. */
public class MenuSteps {

    private final ScenarioContext ctx;

    public MenuSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @Given("the supplier confirms ingredient availability")
    public void theSupplierIsAvailable() {
        BreakfastBackends.supplier().setAvailabilityStatus(200);
        ctx.client().delete("/menu/cache");
    }

    @Given("the supplier is unavailable")
    public void theSupplierIsUnavailable() {
        BreakfastBackends.supplier().setAvailabilityStatus(503);
        ctx.client().delete("/menu/cache");
    }

    @When("the menu is requested")
    public void theMenuIsRequested() {
        ctx.lastResponse = ctx.client().get("/menu");
    }

    @Then("every menu item is available")
    public void everyMenuItemIsAvailable() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(menu()).allMatch(MenuItemResponse::isAvailable);
    }

    @Then("every menu item is unavailable")
    public void everyMenuItemIsUnavailable() {
        assertThat(ctx.lastResponse.status()).isEqualTo(200);
        assertThat(menu()).noneMatch(MenuItemResponse::isAvailable);
    }

    private List<MenuItemResponse> menu() {
        return ctx.lastResponse.as(new TypeReference<List<MenuItemResponse>>() { });
    }
}
