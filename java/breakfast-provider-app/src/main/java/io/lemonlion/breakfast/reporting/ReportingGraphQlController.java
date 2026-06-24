package io.lemonlion.breakfast.reporting;

import io.lemonlion.breakfast.storage.BatchCompletionRecordRepository;
import io.lemonlion.breakfast.storage.IngredientShipmentRepository;
import io.lemonlion.breakfast.storage.OrderSummaryRepository;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import org.springframework.graphql.data.method.annotation.QueryMapping;
import org.springframework.stereotype.Controller;

/** Twin of C# HotChocolate {@code ReportingQuery}: GraphQL query API over the reporting store. */
@Controller
public class ReportingGraphQlController {

    private final OrderSummaryRepository orderSummaries;
    private final IngredientShipmentRepository ingredientShipments;
    private final BatchCompletionRecordRepository batchCompletions;

    public ReportingGraphQlController(OrderSummaryRepository orderSummaries,
                                      IngredientShipmentRepository ingredientShipments,
                                      BatchCompletionRecordRepository batchCompletions) {
        this.orderSummaries = orderSummaries;
        this.ingredientShipments = ingredientShipments;
        this.batchCompletions = batchCompletions;
    }

    @QueryMapping
    public List<OrderSummaryView> orderSummaries() {
        return orderSummaries.findAll().stream()
                .map(o -> new OrderSummaryView(
                        o.getOrderId().toString(), o.getCustomerName(), o.getItemCount(),
                        o.getTableNumber(), o.getStatus(),
                        o.getCreatedAt() == null ? null : o.getCreatedAt().toString()))
                .toList();
    }

    @QueryMapping
    public List<RecipeTypeCount> popularRecipes() {
        Map<String, Integer> counts = new LinkedHashMap<>();
        for (var summary : orderSummaries.findAll()) {
            String types = summary.getRecipeTypes();
            if (types == null || types.isBlank()) {
                continue;
            }
            for (String type : types.split(",")) {
                String trimmed = type.trim();
                if (!trimmed.isEmpty()) {
                    counts.merge(trimmed, 1, Integer::sum);
                }
            }
        }
        return counts.entrySet().stream()
                .sorted(Map.Entry.<String, Integer>comparingByValue().reversed())
                .map(e -> new RecipeTypeCount(e.getKey(), e.getValue()))
                .toList();
    }

    @QueryMapping
    public List<IngredientShipmentView> ingredientShipments() {
        return ingredientShipments.findAll().stream()
                .map(s -> new IngredientShipmentView(
                        s.getDeliveryId().toString(), s.getIngredientName(), s.getQuantity(),
                        s.getDeliveredAt() == null ? null : s.getDeliveredAt().toString()))
                .toList();
    }

    /** GraphQL view of {@code IngredientShipment}. */
    public record IngredientShipmentView(String deliveryId, String ingredientName, double quantity,
                                         String deliveredAt) {
    }

    @QueryMapping
    public List<BatchCompletionView> batchCompletions() {
        return batchCompletions.findAll().stream()
                .map(b -> new BatchCompletionView(
                        b.getBatchId().toString(), b.getRecipeType(),
                        b.getCompletedAt() == null ? null : b.getCompletedAt().toString()))
                .toList();
    }

    /** GraphQL view of {@code BatchCompletionRecord}. */
    public record BatchCompletionView(String batchId, String recipeType, String completedAt) {
    }

    /** GraphQL view of {@code OrderSummary}. */
    public record OrderSummaryView(
            String orderId, String customerName, int itemCount, Integer tableNumber, String status,
            String createdAt) {
    }

    /** GraphQL view of a recipe-type count. */
    public record RecipeTypeCount(String recipeType, int count) {
    }
}
