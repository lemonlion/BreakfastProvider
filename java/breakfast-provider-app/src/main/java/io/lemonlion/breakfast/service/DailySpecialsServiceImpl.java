package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.config.DailySpecialsConfig;
import io.lemonlion.breakfast.events.PubSubPublisher;
import io.lemonlion.breakfast.model.event.DailySpecialOrderedEvent;
import io.lemonlion.breakfast.model.response.DailySpecialOrderResponse;
import io.lemonlion.breakfast.model.response.DailySpecialResponse;
import io.lemonlion.breakfast.storage.IdempotencyStore;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import org.springframework.stereotype.Service;

/**
 * Twin of C# {@code DailySpecialsService}: a fixed set of daily specials with per-special order counters,
 * idempotent ordering (Cosmos idempotency store) and a Pub/Sub ordered event.
 */
@Service
public class DailySpecialsServiceImpl implements DailySpecialsService {

    private static final List<Special> SPECIALS = List.of(
            new Special(UUID.fromString("aaaa0000-0000-0000-0000-000000000001"),
                    "Cinnamon Swirl Pancakes", "Fluffy pancakes with cinnamon sugar swirl and cream cheese drizzle"),
            new Special(UUID.fromString("aaaa0000-0000-0000-0000-000000000002"),
                    "Matcha Waffles", "Crispy green tea waffles with white chocolate chips"),
            new Special(UUID.fromString("aaaa0000-0000-0000-0000-000000000003"),
                    "Lemon Ricotta Pancakes", "Light and airy pancakes with fresh ricotta and lemon zest"));

    private final ConcurrentHashMap<UUID, Integer> orderCounts = new ConcurrentHashMap<>();

    private final DailySpecialsConfig config;
    private final IdempotencyStore idempotencyStore;
    private final PubSubPublisher pubSubPublisher;

    public DailySpecialsServiceImpl(DailySpecialsConfig config, IdempotencyStore idempotencyStore,
                                    PubSubPublisher pubSubPublisher) {
        this.config = config;
        this.idempotencyStore = idempotencyStore;
        this.pubSubPublisher = pubSubPublisher;
    }

    @Override
    public List<DailySpecialResponse> getAvailableSpecials() {
        int max = config.getMaxOrdersPerSpecial();
        return SPECIALS.stream()
                .map(s -> new DailySpecialResponse(s.id(), s.name(), s.description(),
                        Math.max(0, max - orderCounts.getOrDefault(s.id(), 0))))
                .toList();
    }

    @Override
    public Optional<DailySpecialOrderResponse> checkIdempotency(String idempotencyKey) {
        if (idempotencyKey == null) {
            return Optional.empty();
        }
        return idempotencyStore.tryGet(idempotencyKey, DailySpecialOrderResponse.class);
    }

    @Override
    public Optional<Special> validateSpecialExists(UUID specialId) {
        return SPECIALS.stream().filter(s -> s.id().equals(specialId)).findFirst();
    }

    @Override
    public synchronized Optional<DailySpecialOrderResponse> reserveQuantity(UUID specialId, int quantity,
                                                                            String specialName) {
        int max = config.getMaxOrdersPerSpecial();
        int current = orderCounts.getOrDefault(specialId, 0);
        if (current + quantity > max) {
            return Optional.empty();
        }
        int newCount = current + quantity;
        orderCounts.put(specialId, newCount);
        return Optional.of(new DailySpecialOrderResponse(
                UUID.randomUUID(), specialId, quantity, Math.max(0, max - newCount)));
    }

    @Override
    public void storeIdempotencyResult(String idempotencyKey, DailySpecialOrderResponse response) {
        if (idempotencyKey != null) {
            idempotencyStore.set(idempotencyKey, 201, response);
        }
    }

    @Override
    public void publishOrderEvent(DailySpecialOrderResponse response, String specialName) {
        pubSubPublisher.publish(new DailySpecialOrderedEvent(
                response.orderConfirmationId(), specialName, "Guest", response.remainingQuantity(), Instant.now()));
    }

    @Override
    public void resetOrderCounts(UUID specialId) {
        if (specialId != null) {
            orderCounts.remove(specialId);
        } else {
            orderCounts.clear();
        }
    }
}
