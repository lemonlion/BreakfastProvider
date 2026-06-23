package io.lemonlion.breakfast.reporting;

import io.lemonlion.breakfast.storage.OrderSummaryRepository;
import java.util.List;
import org.springframework.graphql.data.method.annotation.QueryMapping;
import org.springframework.stereotype.Controller;

/** Twin of C# HotChocolate {@code ReportingQuery}: GraphQL query API over the reporting store. */
@Controller
public class ReportingGraphQlController {

    private final OrderSummaryRepository orderSummaries;

    public ReportingGraphQlController(OrderSummaryRepository orderSummaries) {
        this.orderSummaries = orderSummaries;
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
        return List.of();
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
