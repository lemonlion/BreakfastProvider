package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.events.PubSubPublisher;
import io.lemonlion.breakfast.model.event.InventoryItemAddedEvent;
import io.lemonlion.breakfast.model.event.InventoryStockUpdatedEvent;
import io.lemonlion.breakfast.model.request.InventoryItemRequest;
import io.lemonlion.breakfast.model.response.InventoryItemResponse;
import io.lemonlion.breakfast.storage.InventoryItemEntity;
import io.lemonlion.breakfast.storage.InventoryItemRepository;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/** Twin of C# {@code InventoryService} (relational/JPA + Pub/Sub item-added and stock-updated events). */
@Service
public class InventoryServiceImpl implements InventoryService {

    private final InventoryItemRepository repository;
    private final PubSubPublisher pubSubPublisher;

    public InventoryServiceImpl(InventoryItemRepository repository, PubSubPublisher pubSubPublisher) {
        this.repository = repository;
        this.pubSubPublisher = pubSubPublisher;
    }

    @Override
    @Transactional
    public InventoryItemResponse create(InventoryItemRequest request) {
        Instant now = Instant.now();
        InventoryItemEntity entity = new InventoryItemEntity();
        entity.setName(request.name());
        entity.setCategory(request.category());
        entity.setQuantity(request.quantity());
        entity.setUnit(request.unit());
        entity.setReorderLevel(request.reorderLevel());
        entity.setLastRestockedAt(now);
        entity.setCreatedAt(now);
        InventoryItemEntity saved = repository.save(entity);

        pubSubPublisher.publish(new InventoryItemAddedEvent(saved.getId(), saved.getName(), saved.getCategory(),
                saved.getQuantity(), saved.getUnit(), saved.getCreatedAt()));

        return toResponse(saved);
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<InventoryItemResponse> getById(int id) {
        return repository.findById(id).map(InventoryServiceImpl::toResponse);
    }

    @Override
    @Transactional(readOnly = true)
    public List<InventoryItemResponse> list() {
        return repository.findAllByOrderByNameAsc().stream().map(InventoryServiceImpl::toResponse).toList();
    }

    @Override
    @Transactional
    public Optional<InventoryItemResponse> update(int id, InventoryItemRequest request) {
        Optional<InventoryItemEntity> found = repository.findById(id);
        if (found.isEmpty()) {
            return Optional.empty();
        }
        InventoryItemEntity entity = found.get();
        BigDecimal previousQuantity = entity.getQuantity();

        entity.setName(request.name());
        entity.setCategory(request.category());
        entity.setQuantity(request.quantity());
        entity.setUnit(request.unit());
        entity.setReorderLevel(request.reorderLevel());
        entity.setLastRestockedAt(Instant.now());
        InventoryItemEntity saved = repository.save(entity);

        if (previousQuantity.compareTo(saved.getQuantity()) != 0) {
            pubSubPublisher.publish(new InventoryStockUpdatedEvent(saved.getId(), saved.getName(),
                    previousQuantity, saved.getQuantity(), Instant.now()));
        }

        return Optional.of(toResponse(saved));
    }

    @Override
    @Transactional
    public boolean delete(int id) {
        if (!repository.existsById(id)) {
            return false;
        }
        repository.deleteById(id);
        return true;
    }

    private static InventoryItemResponse toResponse(InventoryItemEntity entity) {
        return new InventoryItemResponse(entity.getId(), entity.getName(), entity.getCategory(), entity.getQuantity(),
                entity.getUnit(), entity.getReorderLevel(), entity.getLastRestockedAt(), entity.getCreatedAt());
    }
}
