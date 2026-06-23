package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.events.PubSubPublisher;
import io.lemonlion.breakfast.model.event.StaffMemberAddedEvent;
import io.lemonlion.breakfast.model.request.StaffMemberRequest;
import io.lemonlion.breakfast.model.response.StaffMemberResponse;
import io.lemonlion.breakfast.storage.StaffMemberEntity;
import io.lemonlion.breakfast.storage.StaffMemberRepository;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/** Twin of C# {@code StaffService} (relational/JPA persistence + Pub/Sub on create). */
@Service
public class StaffServiceImpl implements StaffService {

    private final StaffMemberRepository repository;
    private final PubSubPublisher pubSubPublisher;

    public StaffServiceImpl(StaffMemberRepository repository, PubSubPublisher pubSubPublisher) {
        this.repository = repository;
        this.pubSubPublisher = pubSubPublisher;
    }

    @Override
    @Transactional
    public StaffMemberResponse create(StaffMemberRequest request) {
        Instant now = Instant.now();
        StaffMemberEntity entity = new StaffMemberEntity();
        entity.setName(request.name());
        entity.setRole(request.role());
        entity.setEmail(request.email());
        entity.setActive(request.activeOrDefault());
        entity.setHiredAt(request.hiredAt() == null ? now : request.hiredAt());
        entity.setCreatedAt(now);
        StaffMemberEntity saved = repository.save(entity);

        pubSubPublisher.publish(new StaffMemberAddedEvent(
                saved.getId(), saved.getName(), saved.getRole(), saved.getCreatedAt()));

        return toResponse(saved);
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<StaffMemberResponse> getById(int id) {
        return repository.findById(id).map(StaffServiceImpl::toResponse);
    }

    @Override
    @Transactional(readOnly = true)
    public List<StaffMemberResponse> list() {
        return repository.findAllByOrderByNameAsc().stream().map(StaffServiceImpl::toResponse).toList();
    }

    @Override
    @Transactional
    public Optional<StaffMemberResponse> update(int id, StaffMemberRequest request) {
        Optional<StaffMemberEntity> found = repository.findById(id);
        if (found.isEmpty()) {
            return Optional.empty();
        }
        StaffMemberEntity entity = found.get();
        entity.setName(request.name());
        entity.setRole(request.role());
        entity.setEmail(request.email());
        entity.setActive(request.activeOrDefault());
        if (request.hiredAt() != null) {
            entity.setHiredAt(request.hiredAt());
        }
        return Optional.of(toResponse(repository.save(entity)));
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

    private static StaffMemberResponse toResponse(StaffMemberEntity entity) {
        return new StaffMemberResponse(entity.getId(), entity.getName(), entity.getRole(), entity.getEmail(),
                entity.isActive(), entity.getHiredAt(), entity.getCreatedAt());
    }
}
