package io.lemonlion.breakfast.storage;

/** Cosmos document for a cached idempotent response (twin of the C# idempotency store entry). */
public class IdempotencyRecord {

    private String id = "";
    private String partitionKey = "";
    private String docType = "idempotency";
    private int statusCode;
    private String payload = "";

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

    public int getStatusCode() {
        return statusCode;
    }

    public void setStatusCode(int statusCode) {
        this.statusCode = statusCode;
    }

    public String getPayload() {
        return payload;
    }

    public void setPayload(String payload) {
        this.payload = payload;
    }
}
