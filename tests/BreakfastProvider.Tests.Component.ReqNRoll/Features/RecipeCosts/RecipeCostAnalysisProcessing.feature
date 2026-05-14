Feature: Recipe Cost Analysis Processing
    Kafka → BreakfastProvider → BigQuery + gRPC + HTTP: Recipe cost event consumption and downstream processing

    @happy-path @IgnoreIfExternalSut
    Scenario: Consuming recipe cost event should trigger downstream processing
        Given a recipe cost calculated event
        When the event is published to Kafka
        Then the calculation ID should be generated
        And the kitchen service should have received the preparation request
