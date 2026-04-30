Feature: Stream Order Updates via gRPC
    /grpc - Streaming order updates via gRPC server streaming

    @happy-path
    Scenario: Streaming order updates should return the current status
        Given a pancake batch has been created
        And an order has been created for the batch
        When order updates are streamed via gRPC
        Then the streamed response should contain the order status

    Scenario: Streaming updates for non-existent order should return not found
        When order updates for a non-existent order are streamed via gRPC
        Then the gRPC stream should return a not found error
