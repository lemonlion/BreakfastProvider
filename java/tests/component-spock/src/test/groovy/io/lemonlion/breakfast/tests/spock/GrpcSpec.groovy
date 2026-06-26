package io.lemonlion.breakfast.tests.spock

import io.grpc.Status
import io.grpc.StatusRuntimeException
import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.grpc.OrderStatusReply
import io.lemonlion.breakfast.grpc.OrderStatusRequest
import io.lemonlion.breakfast.grpc.RecipeSummaryRequest
import io.lemonlion.breakfast.model.request.OrderItemRequest
import io.lemonlion.breakfast.model.request.OrderRequest
import io.lemonlion.breakfast.model.response.OrderResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import io.lemonlion.breakfast.testsupport.GrpcSupport
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Grpc domain component spec (Spock) — twin of the C# BreakfastGrpcService scenarios. */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class GrpcSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    def "recipe summary for #recipeType returns #batches batches and its ingredients"() {
        when:
        def reply = GrpcSupport.blockingStub()
                .getRecipeSummary(RecipeSummaryRequest.newBuilder().setRecipeType(recipeType).build())

        then:
        reply.recipeType == recipeType
        reply.totalBatches == batches
        reply.commonIngredientsList == ingredients

        where:
        recipeType | batches | ingredients
        "Pancakes" | 42      | ["Milk", "Flour", "Eggs"]
        "Waffles"  | 28      | ["Milk", "Flour", "Eggs", "Butter"]
        "Unknown"  | 0       | []
    }

    def "order status returns the created order's details"() {
        given:
        def customer = "Cust-${UUID.randomUUID()}"
        OrderResponse order = client.post("/orders",
                new OrderRequest(customer, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 2)], 5))
                .as(OrderResponse)

        when:
        OrderStatusReply reply = GrpcSupport.blockingStub()
                .getOrderStatus(OrderStatusRequest.newBuilder().setOrderId(order.orderId().toString()).build())

        then:
        reply.orderId == order.orderId().toString()
        reply.customerName == customer
        reply.status == "Created"
        reply.itemCount == 1
    }

    def "order status for a non-existent order is a NOT_FOUND error"() {
        when:
        GrpcSupport.blockingStub()
                .getOrderStatus(OrderStatusRequest.newBuilder().setOrderId(UUID.randomUUID().toString()).build())

        then:
        def ex = thrown(StatusRuntimeException)
        ex.status.code == Status.Code.NOT_FOUND
    }

    def "stream order updates emits the current status for an existing order"() {
        given:
        def customer = "Cust-${UUID.randomUUID()}"
        OrderResponse order = client.post("/orders",
                new OrderRequest(customer, [new OrderItemRequest("Pancakes", UUID.randomUUID(), 1)], 3))
                .as(OrderResponse)

        when:
        def replies = []
        GrpcSupport.blockingStub()
                .streamOrderUpdates(OrderStatusRequest.newBuilder().setOrderId(order.orderId().toString()).build())
                .forEachRemaining { replies << it }

        then:
        replies.size() == 1
        replies[0].orderId == order.orderId().toString()
        replies[0].status == "Created"
    }

    def "stream order updates for a non-existent order is a NOT_FOUND error"() {
        when:
        GrpcSupport.blockingStub()
                .streamOrderUpdates(OrderStatusRequest.newBuilder().setOrderId(UUID.randomUUID().toString()).build())
                .forEachRemaining { }

        then:
        def ex = thrown(StatusRuntimeException)
        ex.status.code == Status.Code.NOT_FOUND
    }
}
