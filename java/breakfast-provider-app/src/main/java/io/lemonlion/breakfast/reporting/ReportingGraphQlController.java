package io.lemonlion.breakfast.reporting;

import io.lemonlion.breakfast.storage.BatchCompletionRecordRepository;
import io.lemonlion.breakfast.storage.EquipmentAlertRepository;
import io.lemonlion.breakfast.storage.IngredientShipmentRepository;
import io.lemonlion.breakfast.storage.OrderSummaryRepository;
import io.lemonlion.breakfast.storage.RecipeReportRepository;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import org.springframework.graphql.data.method.annotation.QueryMapping;
import org.springframework.stereotype.Controller;

/** Twin of C# HotChocolate {@code ReportingQuery}: GraphQL query API over the reporting store. */
@Controller
public class ReportingGraphQlController {

    private final OrderSummaryRepository orderSummaries;
    private final IngredientShipmentRepository ingredientShipments;
    private final BatchCompletionRecordRepository batchCompletions;
    private final EquipmentAlertRepository equipmentAlerts;
    private final RecipeReportRepository recipeReports;

    public ReportingGraphQlController(OrderSummaryRepository orderSummaries,
                                      IngredientShipmentRepository ingredientShipments,
                                      BatchCompletionRecordRepository batchCompletions,
                                      EquipmentAlertRepository equipmentAlerts,
                                      RecipeReportRepository recipeReports) {
        this.orderSummaries = orderSummaries;
        this.ingredientShipments = ingredientShipments;
        this.batchCompletions = batchCompletions;
        this.equipmentAlerts = equipmentAlerts;
        this.recipeReports = recipeReports;
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

    @QueryMapping
    public List<EquipmentAlertView> equipmentAlerts() {
        return equipmentAlerts.findAll().stream()
                .map(a -> new EquipmentAlertView(
                        a.getAlertId().toString(), a.getBatchId().toString(), a.getEquipmentName(),
                        a.getAlertType(), a.getAlertedAt() == null ? null : a.getAlertedAt().toString()))
                .toList();
    }

    /** GraphQL view of {@code EquipmentAlert}. */
    public record EquipmentAlertView(String alertId, String batchId, String equipmentName, String alertType,
                                     String alertedAt) {
    }

    @QueryMapping
    public List<RecipeReportView> recipeReports() {
        return recipeReports.findAll().stream()
                .map(r -> new RecipeReportView(
                        r.getOrderId().toString(), r.getRecipeType(), r.getIngredients(), r.getToppings(),
                        r.getLoggedAt() == null ? null : r.getLoggedAt().toString()))
                .toList();
    }

    /** GraphQL view of {@code RecipeReport}. */
    public record RecipeReportView(String orderId, String recipeType, String ingredients, String toppings,
                                   String loggedAt) {
    }

    @QueryMapping
    public List<IngredientUsageCount> ingredientUsage() {
        // Aggregate ingredient occurrences across all recipe reports, grouped case-insensitively and
        // ordered by descending count (twin of the C# ReportingQuery.GetIngredientUsage).
        Map<String, int[]> counts = new LinkedHashMap<>();
        Map<String, String> display = new LinkedHashMap<>();
        for (var report : recipeReports.findAll()) {
            String ingredients = report.getIngredients();
            if (ingredients == null || ingredients.isBlank()) {
                continue;
            }
            for (String raw : ingredients.split(",")) {
                String trimmed = raw.trim();
                if (trimmed.isEmpty()) {
                    continue;
                }
                String key = trimmed.toLowerCase(Locale.ROOT);
                counts.computeIfAbsent(key, k -> new int[1])[0]++;
                display.putIfAbsent(key, trimmed);
            }
        }
        return counts.entrySet().stream()
                .sorted(Map.Entry.comparingByValue((a, b) -> Integer.compare(b[0], a[0])))
                .map(e -> new IngredientUsageCount(display.get(e.getKey()), e.getValue()[0]))
                .toList();
    }

    /** GraphQL view of an aggregated ingredient-usage count. */
    public record IngredientUsageCount(String ingredient, int count) {
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
