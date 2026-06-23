package io.lemonlion.breakfast.storage;

/** Twin of C# {@code OutboxMessageStatus} constants. */
public final class OutboxMessageStatus {

    public static final String PENDING = "Pending";
    public static final String PROCESSING = "Processing";
    public static final String PROCESSED = "Processed";
    public static final String FAILED = "Failed";

    private OutboxMessageStatus() {
    }
}
