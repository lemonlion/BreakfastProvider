package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.downstream.KitchenClient;
import io.lemonlion.breakfast.events.RecipeLogPublisher;
import io.lemonlion.breakfast.events.outbox.OutboxDestinations;
import io.lemonlion.breakfast.events.outbox.OutboxWriter;
import io.lemonlion.breakfast.model.event.OrderCreatedEvent;
import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.response.OrderItemResponse;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.model.response.PaginatedResponse;
import io.lemonlion.breakfast.notification.NotificationClient;
import io.lemonlion.breakfast.persistence.CosmosRepository;
import io.lemonlion.breakfast.persistence.PagedItems;
import io.lemonlion.breakfast.reporting.ReportingIngester;
import io.lemonlion.breakfast.storage.AuditLogDocument;
import io.lemonlion.breakfast.storage.OrderDocument;
import io.lemonlion.breakfast.storage.OrderItemDocument;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

/**
 * Twin of C# {@code OrderService}. Order creation is an atomic outbox write (order + event), then a set
 * of fire-and-forget side effects (audit log, kitchen prep, gRPC confirmation, recipe log, reporting) that
 * must never roll back the committed order.
 */
@Service
public class OrderServiceImpl implements OrderService {

    private static final Logger log = LoggerFactory.getLogger(OrderServiceImpl.class);

    private final CosmosRepository<OrderDocument> orderRepository;
    private final CosmosRepository<AuditLogDocument> auditLogRepository;
    private final OutboxWriter outboxWriter;
    private final KitchenClient kitchenClient;
    private final NotificationClient notificationClient;
    private final RecipeLogPublisher recipeLogPublisher;
    private final ReportingIngester reportingIngester;

    public OrderServiceImpl(CosmosRepository<OrderDocument> orderRepository,
                            CosmosRepository<AuditLogDocument> auditLogRepository,
                            OutboxWriter outboxWriter,
                            KitchenClient kitchenClient,
                            NotificationClient notificationClient,
                            RecipeLogPublisher recipeLogPublisher,
                            ReportingIngester reportingIngester) {
        this.orderRepository = orderRepository;
        this.auditLogRepository = auditLogRepository;
        this.outboxWriter = outboxWriter;
        this.kitchenClient = kitchenClient;
        this.notificationClient = notificationClient;
        this.recipeLogPublisher = recipeLogPublisher;
        this.reportingIngester = reportingIngester;
    }

    @Override
    public OrderResponse createOrder(OrderRequest request) {
        UUID orderId = UUID.randomUUID();
        Instant now = Instant.now();
        String partitionKey = orderId.toString();

        OrderDocument document = new OrderDocument();
        document.setId(partitionKey);
        document.setPartitionKey(partitionKey);
        document.setOrderId(orderId);
        document.setCustomerName(request.customerName());
        document.setItems(request.items().stream().map(i -> {
            OrderItemDocument item = new OrderItemDocument();
            item.setItemType(i.itemType());
            item.setBatchId(i.batchId());
            item.setQuantity(i.effectiveQuantity());
            return item;
        }).toList());
        document.setTableNumber(request.tableNumber());
        document.setStatus(OrderStatusTransitions.CREATED);
        document.setCreatedAt(now);

        OrderCreatedEvent event = new OrderCreatedEvent(
                orderId, request.customerName(), request.items().size(), request.tableNumber(), now);

        // Atomic: order document + outbox message in one transactional batch.
        outboxWriter.write(document, event, partitionKey, OutboxDestinations.EVENT_GRID);

        log.info("Order {} created for customer {} with {} items at table {}",
                orderId, request.customerName(), request.items().size(), request.tableNumber());

        writeAuditLog("Created", orderId,
                "Order created for " + request.customerName() + " with " + request.items().size() + " items");

        // Fire-and-forget side effects: the order is already committed.
        safely("kitchen preparation", orderId, () -> kitchenClient.requestPreparation(orderId, request));
        safely("order confirmation notification", orderId,
                () -> notificationClient.sendOrderConfirmation(orderId, request.customerName(), request.items().size()));
        safely("recipe log", orderId,
                () -> recipeLogPublisher.publishOrderRecipeLog(orderId, request.customerName(), request.items().size()));
        safely("reporting ingest", orderId,
                () -> reportingIngester.ingestOrderCreated(orderId, request.customerName(),
                        request.items().size(), request.tableNumber(), now));

        return mapToResponse(document);
    }

    @Override
    public Optional<OrderResponse> getOrder(UUID orderId) {
        return orderRepository.findById(orderId.toString(), orderId.toString()).map(OrderServiceImpl::mapToResponse);
    }

    @Override
    public PaginatedResponse<OrderResponse> listOrders(int page, int pageSize) {
        int offset = (page - 1) * pageSize;
        PagedItems<OrderDocument> paged = orderRepository.queryPaged(offset, pageSize);
        List<OrderResponse> items = paged.items().stream().map(OrderServiceImpl::mapToResponse).toList();
        int totalPages = pageSize == 0 ? 0 : (int) Math.ceil((double) paged.totalCount() / pageSize);
        return new PaginatedResponse<>(items, page, pageSize, paged.totalCount(), totalPages);
    }

    @Override
    public StatusUpdateResult updateOrderStatus(UUID orderId, String newStatus) {
        Optional<OrderDocument> found = orderRepository.findById(orderId.toString(), orderId.toString());
        if (found.isEmpty()) {
            return StatusUpdateResult.notFoundResult();
        }
        OrderDocument document = found.get();
        String previous = document.getStatus();
        if (!OrderStatusTransitions.isValid(previous, newStatus)) {
            return StatusUpdateResult.invalid(
                    "Cannot transition from '" + previous + "' to '" + newStatus + "'.");
        }
        document.setStatus(newStatus);
        orderRepository.upsert(document, document.getPartitionKey());
        log.info("Order {} status changed from {} to {}", orderId, previous, newStatus);
        writeAuditLog("StatusChanged", orderId,
                "Order status changed from " + previous + " to " + newStatus);
        return StatusUpdateResult.ok(mapToResponse(document));
    }

    private void writeAuditLog(String action, UUID orderId, String details) {
        AuditLogDocument audit = new AuditLogDocument();
        audit.setPartitionKey("Order");
        audit.setAuditLogId(UUID.randomUUID());
        audit.setAction(action);
        audit.setEntityType("Order");
        audit.setEntityId(orderId);
        audit.setDetails(details);
        audit.setTimestamp(Instant.now());
        auditLogRepository.create(audit, audit.getPartitionKey());
    }

    private static void safely(String what, UUID orderId, Runnable action) {
        try {
            action.run();
        } catch (RuntimeException ex) {
            log.warn("{} failed for order {}; order is committed", what, orderId, ex);
        }
    }

    private static OrderResponse mapToResponse(OrderDocument document) {
        List<OrderItemResponse> items = document.getItems().stream()
                .map(i -> new OrderItemResponse(i.getItemType(), i.getBatchId(), i.getQuantity()))
                .toList();
        return new OrderResponse(
                document.getOrderId(),
                document.getCustomerName(),
                items,
                document.getTableNumber(),
                document.getStatus(),
                document.getCreatedAt());
    }
}
