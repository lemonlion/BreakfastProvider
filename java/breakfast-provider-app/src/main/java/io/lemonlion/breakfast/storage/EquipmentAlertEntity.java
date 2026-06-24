package io.lemonlion.breakfast.storage;

import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.Instant;
import java.util.UUID;

/** Reporting projection of an equipment alert (twin of C# {@code EquipmentAlert}). */
@Entity
@Table(name = "equipment_alerts")
public class EquipmentAlertEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private int id;

    private UUID alertId;
    private UUID batchId;
    private String equipmentName = "";
    private String alertType = "";
    private Instant alertedAt;

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public UUID getAlertId() {
        return alertId;
    }

    public void setAlertId(UUID alertId) {
        this.alertId = alertId;
    }

    public UUID getBatchId() {
        return batchId;
    }

    public void setBatchId(UUID batchId) {
        this.batchId = batchId;
    }

    public String getEquipmentName() {
        return equipmentName;
    }

    public void setEquipmentName(String equipmentName) {
        this.equipmentName = equipmentName;
    }

    public String getAlertType() {
        return alertType;
    }

    public void setAlertType(String alertType) {
        this.alertType = alertType;
    }

    public Instant getAlertedAt() {
        return alertedAt;
    }

    public void setAlertedAt(Instant alertedAt) {
        this.alertedAt = alertedAt;
    }
}
