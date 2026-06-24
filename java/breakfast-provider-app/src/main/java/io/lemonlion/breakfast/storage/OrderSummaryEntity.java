package io.lemonlion.breakfast.storage;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.Instant;
import java.util.UUID;

/** Reporting projection of a created order (twin of C# {@code OrderSummary}). */
@Entity
@Table(name = "order_summaries")
public class OrderSummaryEntity {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private int id;

    private UUID orderId;
    private String customerName = "";
    private int itemCount;
    private Integer tableNumber;
    private String status = "Created";
    private Instant createdAt;

    /** Comma-separated recipe/item types from the order, used to aggregate popular recipes. */
    @Column(length = 2000)
    private String recipeTypes = "";

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public UUID getOrderId() {
        return orderId;
    }

    public void setOrderId(UUID orderId) {
        this.orderId = orderId;
    }

    public String getCustomerName() {
        return customerName;
    }

    public void setCustomerName(String customerName) {
        this.customerName = customerName;
    }

    public int getItemCount() {
        return itemCount;
    }

    public void setItemCount(int itemCount) {
        this.itemCount = itemCount;
    }

    public Integer getTableNumber() {
        return tableNumber;
    }

    public void setTableNumber(Integer tableNumber) {
        this.tableNumber = tableNumber;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(Instant createdAt) {
        this.createdAt = createdAt;
    }

    public String getRecipeTypes() {
        return recipeTypes;
    }

    public void setRecipeTypes(String recipeTypes) {
        this.recipeTypes = recipeTypes;
    }
}
