package io.lemonlion.breakfast.grpc;

import io.grpc.Status;
import io.grpc.stub.StreamObserver;
import io.lemonlion.breakfast.persistence.CosmosRepository;
import io.lemonlion.breakfast.storage.OrderDocument;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import net.devh.boot.grpc.server.service.GrpcService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Twin of the C# {@code BreakfastGrpcService} (Grpc.AspNetCore). Exposes recipe summaries (hard-coded
 * per recipe type) and order status lookups (read from the Cosmos {@code orders} container). Registered
 * as a net.devh {@link GrpcService} so the Kronikol4J server interceptor attributes its work to the
 * calling test.
 */
@GrpcService
public class BreakfastGrpcService extends BreakfastGrpcGrpc.BreakfastGrpcImplBase {

    private static final Logger log = LoggerFactory.getLogger(BreakfastGrpcService.class);

    private final CosmosRepository<OrderDocument> orderRepository;

    public BreakfastGrpcService(CosmosRepository<OrderDocument> orderRepository) {
        this.orderRepository = orderRepository;
    }

    @Override
    public void getRecipeSummary(RecipeSummaryRequest request,
                                 StreamObserver<RecipeSummaryReply> responseObserver) {
        log.info("gRPC GetRecipeSummary called for {}", request.getRecipeType());

        int totalBatches = switch (request.getRecipeType()) {
            case "Pancakes" -> 42;
            case "Waffles" -> 28;
            default -> 0;
        };
        List<String> ingredients = switch (request.getRecipeType()) {
            case "Pancakes" -> List.of("Milk", "Flour", "Eggs");
            case "Waffles" -> List.of("Milk", "Flour", "Eggs", "Butter");
            default -> List.of();
        };

        RecipeSummaryReply reply = RecipeSummaryReply.newBuilder()
                .setRecipeType(request.getRecipeType())
                .setTotalBatches(totalBatches)
                .addAllCommonIngredients(ingredients)
                .setLastPreparedAt(Instant.now().toString())
                .build();

        responseObserver.onNext(reply);
        responseObserver.onCompleted();
    }

    @Override
    public void getOrderStatus(OrderStatusRequest request,
                               StreamObserver<OrderStatusReply> responseObserver) {
        log.info("gRPC GetOrderStatus called for {}", request.getOrderId());

        Optional<OrderStatusReply> reply = lookupOrder(request.getOrderId());
        if (reply.isEmpty()) {
            responseObserver.onError(notFound(request.getOrderId()));
            return;
        }
        responseObserver.onNext(reply.get());
        responseObserver.onCompleted();
    }

    @Override
    public void streamOrderUpdates(OrderStatusRequest request,
                                   StreamObserver<OrderStatusReply> responseObserver) {
        log.info("gRPC StreamOrderUpdates started for {}", request.getOrderId());

        Optional<OrderStatusReply> reply = lookupOrder(request.getOrderId());
        if (reply.isEmpty()) {
            responseObserver.onError(notFound(request.getOrderId()));
            return;
        }
        // Send the current status as the first (and only) message, mirroring the C# stream.
        responseObserver.onNext(reply.get());
        responseObserver.onCompleted();
    }

    private Optional<OrderStatusReply> lookupOrder(String orderId) {
        return orderRepository.findById(orderId, orderId).map(order -> OrderStatusReply.newBuilder()
                .setOrderId(order.getOrderId().toString())
                .setStatus(order.getStatus())
                .setCustomerName(order.getCustomerName())
                .setItemCount(order.getItems().size())
                .setCreatedAt(order.getCreatedAt().toString())
                .build());
    }

    private static io.grpc.StatusRuntimeException notFound(String orderId) {
        return Status.NOT_FOUND.withDescription("Order " + orderId + " not found").asRuntimeException();
    }
}
