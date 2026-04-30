Feature: Order Status via gRPC
    /grpc - Retrieving order status via gRPC

    @happy-path
    Scenario: Order status via gRPC should return order details
        Given a pancake batch has been created
        And an order has been created for the batch
        When the order status is requested via gRPC
        Then the gRPC response should contain the order details

    Scenario: Order status for non-existent order should return not found
        When the order status for a non-existent order is requested via gRPC
        Then the gRPC response should be a not found error
