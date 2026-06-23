package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.events.PubSubPublisher;
import io.lemonlion.breakfast.model.event.ReservationCancelledEvent;
import io.lemonlion.breakfast.model.event.ReservationConfirmedEvent;
import io.lemonlion.breakfast.model.request.ReservationRequest;
import io.lemonlion.breakfast.model.response.ReservationResponse;
import io.lemonlion.breakfast.notification.NotificationClient;
import io.lemonlion.breakfast.storage.ReservationEntity;
import io.lemonlion.breakfast.storage.ReservationRepository;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/** Twin of C# {@code ReservationService} (relational/JPA persistence + Pub/Sub + gRPC reminder). */
@Service
public class ReservationServiceImpl implements ReservationService {

    private static final String CANCELLED = "Cancelled";
    private static final Logger log = LoggerFactory.getLogger(ReservationServiceImpl.class);

    private final ReservationRepository repository;
    private final PubSubPublisher pubSubPublisher;
    private final NotificationClient notificationClient;

    public ReservationServiceImpl(ReservationRepository repository, PubSubPublisher pubSubPublisher,
                                  NotificationClient notificationClient) {
        this.repository = repository;
        this.pubSubPublisher = pubSubPublisher;
        this.notificationClient = notificationClient;
    }

    @Override
    @Transactional
    public ReservationResponse create(ReservationRequest request) {
        ReservationEntity entity = new ReservationEntity();
        entity.setCustomerName(request.customerName());
        entity.setTableNumber(request.tableNumber());
        entity.setPartySize(request.partySize());
        entity.setReservedAt(request.reservedAt());
        entity.setStatus("Confirmed");
        entity.setContactPhone(request.contactPhone());
        entity.setCreatedAt(Instant.now());
        ReservationEntity saved = repository.save(entity);

        pubSubPublisher.publish(new ReservationConfirmedEvent(
                saved.getId(), saved.getCustomerName(), saved.getPartySize(), saved.getReservedAt(),
                saved.getCreatedAt()));

        try {
            notificationClient.sendReservationReminder(String.valueOf(saved.getId()), saved.getCustomerName(),
                    saved.getReservedAt(), saved.getTableNumber());
        } catch (RuntimeException ex) {
            log.warn("Reservation reminder failed for reservation {}; reservation is committed", saved.getId(), ex);
        }

        return toResponse(saved);
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<ReservationResponse> getById(int id) {
        return repository.findById(id).map(ReservationServiceImpl::toResponse);
    }

    @Override
    @Transactional(readOnly = true)
    public List<ReservationResponse> list() {
        return repository.findAllByOrderByReservedAtAsc().stream().map(ReservationServiceImpl::toResponse).toList();
    }

    @Override
    @Transactional
    public Optional<ReservationResponse> update(int id, ReservationRequest request) {
        Optional<ReservationEntity> found = repository.findById(id);
        if (found.isEmpty() || CANCELLED.equals(found.get().getStatus())) {
            return Optional.empty();
        }
        ReservationEntity entity = found.get();
        entity.setCustomerName(request.customerName());
        entity.setTableNumber(request.tableNumber());
        entity.setPartySize(request.partySize());
        entity.setReservedAt(request.reservedAt());
        entity.setContactPhone(request.contactPhone());
        return Optional.of(toResponse(repository.save(entity)));
    }

    @Override
    @Transactional
    public CancelResult cancel(int id) {
        Optional<ReservationEntity> found = repository.findById(id);
        if (found.isEmpty()) {
            return CancelResult.notFoundResult();
        }
        ReservationEntity entity = found.get();
        if (CANCELLED.equals(entity.getStatus())) {
            return CancelResult.error("Reservation is already cancelled.");
        }
        entity.setStatus(CANCELLED);
        ReservationEntity saved = repository.save(entity);

        pubSubPublisher.publish(new ReservationCancelledEvent(
                saved.getId(), saved.getCustomerName(), "Cancelled by customer", Instant.now()));

        return CancelResult.ok(toResponse(saved));
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

    private static ReservationResponse toResponse(ReservationEntity entity) {
        return new ReservationResponse(entity.getId(), entity.getCustomerName(), entity.getTableNumber(),
                entity.getPartySize(), entity.getReservedAt(), entity.getStatus(), entity.getContactPhone(),
                entity.getCreatedAt());
    }
}
