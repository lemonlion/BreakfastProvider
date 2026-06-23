package io.lemonlion.breakfast.reporting;

import io.lemonlion.breakfast.storage.OrderSummaryEntity;
import io.lemonlion.breakfast.storage.OrderSummaryRepository;
import java.time.Instant;
import java.util.UUID;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 * Twin of C# {@code ReportingIngester}: persists reporting projections to the relational reporting store
 * (replaces the earlier no-op). Order creation ingests an {@code OrderSummary}, queried via GraphQL.
 */
@Service
public class JpaReportingIngester implements ReportingIngester {

    private final OrderSummaryRepository orderSummaries;

    public JpaReportingIngester(OrderSummaryRepository orderSummaries) {
        this.orderSummaries = orderSummaries;
    }

    @Override
    @Transactional
    public void ingestOrderCreated(UUID orderId, String customerName, int itemCount, Integer tableNumber,
                                   Instant createdAt) {
        if (orderSummaries.findByOrderId(orderId).isPresent()) {
            return;
        }
        OrderSummaryEntity summary = new OrderSummaryEntity();
        summary.setOrderId(orderId);
        summary.setCustomerName(customerName);
        summary.setItemCount(itemCount);
        summary.setTableNumber(tableNumber);
        summary.setStatus("Created");
        summary.setCreatedAt(createdAt);
        orderSummaries.save(summary);
    }
}
