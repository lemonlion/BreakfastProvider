package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.config.FeatureSwitchesConfig;
import io.lemonlion.breakfast.events.PubSubPublisher;
import io.lemonlion.breakfast.model.event.ToppingCreatedEvent;
import io.lemonlion.breakfast.model.request.ToppingRequest;
import io.lemonlion.breakfast.model.request.UpdateToppingRequest;
import io.lemonlion.breakfast.model.response.ToppingResponse;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.stereotype.Service;

/**
 * Twin of C# {@code ToppingService}: a fixed catalogue of toppings with a feature-flag filter
 * (Raspberries), create (publishes a Pub/Sub event), and stateless update/delete over the seed.
 */
@Service
public class ToppingServiceImpl implements ToppingService {

    private static final List<ToppingResponse> SEED = List.of(
            new ToppingResponse(UUID.fromString("11111111-0000-0000-0000-000000000001"), "Raspberries", "Fruit"),
            new ToppingResponse(UUID.fromString("11111111-0000-0000-0000-000000000002"), "Blueberries", "Fruit"),
            new ToppingResponse(UUID.fromString("11111111-0000-0000-0000-000000000003"), "Maple Syrup", "Syrup"),
            new ToppingResponse(UUID.fromString("11111111-0000-0000-0000-000000000004"), "Whipped Cream", "Cream"),
            new ToppingResponse(UUID.fromString("11111111-0000-0000-0000-000000000005"), "Chocolate Chips", "Chocolate"));

    private final FeatureSwitchesConfig featureSwitches;
    private final PubSubPublisher pubSubPublisher;

    public ToppingServiceImpl(FeatureSwitchesConfig featureSwitches, PubSubPublisher pubSubPublisher) {
        this.featureSwitches = featureSwitches;
        this.pubSubPublisher = pubSubPublisher;
    }

    @Override
    public List<ToppingResponse> getAvailableToppings() {
        if (featureSwitches.isRaspberryToppingEnabled()) {
            return SEED;
        }
        return SEED.stream().filter(t -> !"Raspberries".equals(t.name())).toList();
    }

    @Override
    public ToppingResponse createTopping(ToppingRequest request) {
        ToppingResponse topping = new ToppingResponse(UUID.randomUUID(), request.name(), request.category());
        pubSubPublisher.publish(new ToppingCreatedEvent(
                topping.toppingId(), topping.name(), topping.category(), false, Instant.now()));
        return topping;
    }

    @Override
    public Optional<ToppingResponse> updateTopping(UUID toppingId, UpdateToppingRequest request) {
        if (find(toppingId).isEmpty()) {
            return Optional.empty();
        }
        return Optional.of(new ToppingResponse(toppingId, request.name(), request.category()));
    }

    @Override
    public boolean deleteTopping(UUID toppingId) {
        return find(toppingId).isPresent();
    }

    private Optional<ToppingResponse> find(UUID toppingId) {
        return SEED.stream().filter(t -> t.toppingId().equals(toppingId)).findFirst();
    }
}
