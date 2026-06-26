package io.lemonlion.breakfast.reporting;

import io.lemonlion.breakfast.storage.IngredientShipmentEntity;
import io.lemonlion.breakfast.storage.IngredientShipmentRepository;
import io.lemonlion.breakfast.storage.OrderSummaryEntity;
import io.lemonlion.breakfast.storage.OrderSummaryRepository;
import io.lemonlion.breakfast.storage.RecipeReportEntity;
import io.lemonlion.breakfast.storage.RecipeReportRepository;
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
    private final RecipeReportRepository recipeReports;

    public JpaReportingIngester(OrderSummaryRepository orderSummaries,
                                IngredientShipmentRepository ingredientShipments,
                                RecipeReportRepository recipeReports) {
        this.orderSummaries = orderSummaries;
        this.ingredientShipments = ingredientShipments;
        this.recipeReports = recipeReports;
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

    @Override
    @Transactional
    public void ingestRecipeLog(UUID orderId, String recipeType, List<String> ingredients, List<String> toppings,
                                Instant loggedAt) {
        RecipeReportEntity report = new RecipeReportEntity();
        report.setOrderId(orderId);
        // recipeType is exposed as a non-null GraphQL field; default a missing value to "" so the
        // recipeReports query never fails serialization on a null.
        report.setRecipeType(recipeType == null ? "" : recipeType);
        report.setIngredients(ingredients == null ? "" : String.join(",", ingredients));
        report.setToppings(toppings == null ? "" : String.join(",", toppings));
        report.setLoggedAt(loggedAt);
        recipeReports.save(report);
    }
}
