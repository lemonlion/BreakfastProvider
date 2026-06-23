package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.downstream.SupplierClient;
import io.lemonlion.breakfast.events.PubSubPublisher;
import io.lemonlion.breakfast.model.event.MenuAvailabilityChangedEvent;
import io.lemonlion.breakfast.model.response.MenuItemResponse;
import java.time.Duration;
import java.time.Instant;
import java.util.Comparator;
import java.util.List;
import org.springframework.stereotype.Service;

/**
 * Twin of C# {@code MenuService}: a fixed menu whose availability is gated by a Supplier check, cached
 * for 5 minutes (only when available), with a Pub/Sub availability-changed event on each fresh build.
 */
@Service
public class MenuServiceImpl implements MenuService {

    private static final Duration CACHE_TTL = Duration.ofMinutes(5);

    private static final List<MenuItemResponse> MENU = List.of(
            new MenuItemResponse("Classic Pancakes",
                    "Fluffy pancakes made with fresh milk, eggs, and flour",
                    true, List.of("Milk", "Eggs", "Flour")),
            new MenuItemResponse("Belgian Waffles",
                    "Crispy waffles with butter, milk, eggs, and flour",
                    true, List.of("Milk", "Eggs", "Flour", "Butter")),
            new MenuItemResponse("Goat Milk Pancakes",
                    "Specialty pancakes made with fresh goat milk",
                    true, List.of("Goat Milk", "Eggs", "Flour")));

    private final SupplierClient supplierClient;
    private final PubSubPublisher pubSubPublisher;

    private volatile List<MenuItemResponse> cached;
    private volatile Instant cacheExpiry = Instant.MIN;

    public MenuServiceImpl(SupplierClient supplierClient, PubSubPublisher pubSubPublisher) {
        this.supplierClient = supplierClient;
        this.pubSubPublisher = pubSubPublisher;
    }

    @Override
    public List<MenuItemResponse> getMenu() {
        List<MenuItemResponse> snapshot = cached;
        if (snapshot != null && Instant.now().isBefore(cacheExpiry)) {
            return snapshot;
        }

        boolean ingredientsAvailable = supplierClient.isMilkAvailable();
        List<MenuItemResponse> menu = MENU.stream()
                .map(item -> new MenuItemResponse(item.name(), item.description(), ingredientsAvailable,
                        item.requiredIngredients()))
                .sorted(Comparator.comparing(MenuItemResponse::name))
                .toList();

        if (ingredientsAvailable) {
            cached = menu;
            cacheExpiry = Instant.now().plus(CACHE_TTL);
        }

        pubSubPublisher.publish(new MenuAvailabilityChangedEvent("All Items", ingredientsAvailable,
                ingredientsAvailable ? "Supplier confirmed availability" : "Supplier unavailable", Instant.now()));

        return menu;
    }

    @Override
    public void clearCache() {
        cached = null;
        cacheExpiry = Instant.MIN;
    }
}
