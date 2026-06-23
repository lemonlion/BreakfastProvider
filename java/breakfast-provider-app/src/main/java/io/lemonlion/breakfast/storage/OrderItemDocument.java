package io.lemonlion.breakfast.storage;

import java.util.UUID;

/** Twin of C# {@code OrderItemDocument} (embedded in {@link OrderDocument}). */
public class OrderItemDocument {

    private String itemType = "";
    private UUID batchId;
    private int quantity;

    public String getItemType() {
        return itemType;
    }

    public void setItemType(String itemType) {
        this.itemType = itemType;
    }

    public UUID getBatchId() {
        return batchId;
    }

    public void setBatchId(UUID batchId) {
        this.batchId = batchId;
    }

    public int getQuantity() {
        return quantity;
    }

    public void setQuantity(int quantity) {
        this.quantity = quantity;
    }
}
