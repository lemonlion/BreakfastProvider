Feature: Service Time Analysis
    Order served events - Service time analysis (Kafka → ClickHouse → gRPC → Kitchen)

    @happy-path @IgnoreIfExternalSut
    Scenario: Consuming an order served event should trigger downstream processing
        Given an order served event
        When the order served event is published to Kafka
        Then the order ID should be generated
        And the kitchen service should have received the status request
