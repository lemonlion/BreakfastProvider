package io.lemonlion.breakfast.storage;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

/**
 * Twin of C# {@code OrderDocument}, stored in the Cosmos {@code orders} container. The Cosmos Java SDK
 * requires a lowercase {@code id} property; {@code partitionKey} is the container's partition key path.
 */
public class OrderDocument {

    private String id = UUID.randomUUID().toString();
    private String partitionKey = "";
    /** Discriminator so the shared {@code orders} container (orders + outbox) can be queried per type. */
    private String docType = "order";
    private UUID orderId;
    private String customerName = "";
    private List<OrderItemDocument> items = new ArrayList<>();
    private Integer tableNumber;
    private String status = "Created";
    private Instant createdAt = Instant.now();

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getPartitionKey() {
        return partitionKey;
    }

    public void setPartitionKey(String partitionKey) {
        this.partitionKey = partitionKey;
    }

    public String getDocType() {
        return docType;
    }

    public void setDocType(String docType) {
        this.docType = docType;
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

    public List<OrderItemDocument> getItems() {
        return items;
    }

    public void setItems(List<OrderItemDocument> items) {
        this.items = items;
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
}
