package io.lemonlion.breakfast.reporting;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/** Twin of C# {@code IReportingIngester}: records order-created facts into the reporting store. */
public interface ReportingIngester {

    void ingestOrderCreated(UUID orderId, String customerName, int itemCount, Integer tableNumber, Instant createdAt,
                            List<String> recipeTypes);
}
