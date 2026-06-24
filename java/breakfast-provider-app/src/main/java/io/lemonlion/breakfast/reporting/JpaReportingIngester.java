package io.lemonlion.breakfast.reporting;

import io.lemonlion.breakfast.storage.IngredientShipmentEntity;
import io.lemonlion.breakfast.storage.IngredientShipmentRepository;
import io.lemonlion.breakfast.storage.OrderSummaryEntity;
import io.lemonlion.breakfast.storage.OrderSummaryRepository;
import java.time.Instant;
import java.util.List;
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
    private final IngredientShipmentRepository ingredientShipments;

    public JpaReportingIngester(OrderSummaryRepository orderSummaries,
                                IngredientShipmentRepository ingredientShipments) {
        this.orderSummaries = orderSummaries;
        this.ingredientShipments = ingredientShipments;
    }

    @Override
    @Transactional
    public void ingestOrderCreated(UUID orderId, String customerName, int itemCount, Integer tableNumber,
                                   Instant createdAt, List<String> recipeTypes) {
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
        summary.setRecipeTypes(recipeTypes == null ? "" : String.join(",", recipeTypes));
        orderSummaries.save(summary);
    }

    @Override
    @Transactional
    public void ingestIngredientShipment(UUID deliveryId, String ingredientName, double quantity, Instant deliveredAt) {
        IngredientShipmentEntity shipment = new IngredientShipmentEntity();
        shipment.setDeliveryId(deliveryId);
        shipment.setIngredientName(ingredientName);
        shipment.setQuantity(quantity);
        shipment.setDeliveredAt(deliveredAt);
        ingredientShipments.save(shipment);
    }
}
