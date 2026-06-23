package io.lemonlion.breakfast.model.event;

import java.time.Instant;
import java.util.UUID;

/** Twin of C# {@code EquipmentAlertEvent} (an Event Hubs event). */
public record EquipmentAlertEvent(
        UUID alertId, UUID batchId, String equipmentName, String alertType, Instant alertedAt) {
}
