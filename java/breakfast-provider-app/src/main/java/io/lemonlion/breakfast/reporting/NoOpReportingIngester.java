package io.lemonlion.breakfast.reporting;

import java.time.Instant;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

/**
 * Placeholder reporting ingester. The faithful reporting store (twin of C# {@code ReportingIngester} +
 * {@code ReportingDbContext}) lands with the Reporting domain in Phase 3; until then this logs.
 */
@Component
public class NoOpReportingIngester implements ReportingIngester {

    private static final Logger log = LoggerFactory.getLogger(NoOpReportingIngester.class);

    @Override
    public void ingestOrderCreated(UUID orderId, String customerName, int itemCount,
                                   Integer tableNumber, Instant createdAt) {
        log.debug("Reporting ingest: order {} ({} items) at table {}", orderId, itemCount, tableNumber);
    }
}
