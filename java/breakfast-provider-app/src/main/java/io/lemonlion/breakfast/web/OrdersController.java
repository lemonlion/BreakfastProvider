package io.lemonlion.breakfast.web;

import io.lemonlion.breakfast.model.request.OrderRequest;
import io.lemonlion.breakfast.model.request.UpdateOrderStatusRequest;
import io.lemonlion.breakfast.model.response.OrderResponse;
import io.lemonlion.breakfast.model.response.PaginatedResponse;
import io.lemonlion.breakfast.service.OrderService;
import io.lemonlion.breakfast.web.ApiExceptionHandler.InvalidStateTransitionException;
import java.net.URI;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

/** Twin of C# {@code OrdersController} ({@code /orders}). */
@RestController
@RequestMapping(path = "/orders", produces = MediaType.APPLICATION_JSON_VALUE)
public class OrdersController {

    private static final int MAX_PAGE_SIZE = 50;

    private final OrderService orderService;
    private final OrderValidator validator;
    private final OrderRateLimiter rateLimiter;

    public OrdersController(OrderService orderService, OrderValidator validator, OrderRateLimiter rateLimiter) {
        this.orderService = orderService;
        this.validator = validator;
        this.rateLimiter = rateLimiter;
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<OrderResponse> createOrder(@RequestBody OrderRequest request) {
        if (!rateLimiter.tryAcquire()) {
            return ResponseEntity.status(HttpStatus.TOO_MANY_REQUESTS).build();
        }
        validator.validate(request);
        OrderResponse response = orderService.createOrder(request);
        return ResponseEntity.created(URI.create("/orders/" + response.orderId())).body(response);
    }

    @GetMapping
    public PaginatedResponse<OrderResponse> listOrders(
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "10") int pageSize) {
        int effectivePage = Math.max(page, 1);
        int effectivePageSize = Math.min(Math.max(pageSize, 1), MAX_PAGE_SIZE);
        return orderService.listOrders(effectivePage, effectivePageSize);
    }

    @GetMapping("/{orderId}")
    public ResponseEntity<OrderResponse> getOrder(@PathVariable UUID orderId) {
        return orderService.getOrder(orderId)
                .map(ResponseEntity::ok)
                .orElseGet(() -> ResponseEntity.notFound().build());
    }

    @PatchMapping(path = "/{orderId}/status", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<OrderResponse> updateOrderStatus(
            @PathVariable UUID orderId, @RequestBody UpdateOrderStatusRequest request) {
        validator.validate(request);
        OrderService.StatusUpdateResult result = orderService.updateOrderStatus(orderId, request.status());
        if (result.notFound()) {
            return ResponseEntity.notFound().build();
        }
        if (result.error() != null) {
            throw new InvalidStateTransitionException(result.error());
        }
        return ResponseEntity.ok(result.order());
    }
}
