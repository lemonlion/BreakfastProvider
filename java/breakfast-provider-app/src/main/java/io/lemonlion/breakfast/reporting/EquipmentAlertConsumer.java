package io.lemonlion.breakfast.reporting;

import io.lemonlion.breakfast.model.event.EquipmentAlertEvent;
import io.lemonlion.breakfast.storage.EquipmentAlertEntity;
import io.lemonlion.breakfast.storage.EquipmentAlertRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * Twin of the C# {@code EventHubEquipmentAlertConsumerService}: ingests {@link EquipmentAlertEvent}
 * messages (delivered over Azure Event Hubs) into the reporting store, surfaced by the GraphQL
 * {@code equipmentAlerts} query.
 *
 * <p>{@link #ingest} is the message handler. The Event Hubs transport that would call it has no local
 * emulator, so the docker-mode component suite verifies this handler + store + query by invoking
 * {@code ingest} directly; the transport itself is exercised only in external-sut / Azure (see
 * {@code docs/REMAINING_PARITY.md}).
 */
@Component
public class EquipmentAlertConsumer {

    private static final Logger log = LoggerFactory.getLogger(EquipmentAlertConsumer.class);

    private final EquipmentAlertRepository repository;

    public EquipmentAlertConsumer(EquipmentAlertRepository repository) {
        this.repository = repository;
    }

    @Transactional
    public void ingest(EquipmentAlertEvent event) {
        EquipmentAlertEntity alert = new EquipmentAlertEntity();
        alert.setAlertId(event.alertId());
        alert.setBatchId(event.batchId());
        alert.setEquipmentName(event.equipmentName());
        alert.setAlertType(event.alertType());
        alert.setAlertedAt(event.alertedAt());
        repository.save(alert);
        log.info("Ingested equipment alert {} for batch {}", event.alertId(), event.batchId());
    }
}
