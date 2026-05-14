Feature: Customer Feedback Alert Processing
    PubSub → BreakfastProvider → MongoDB + gRPC + HTTP: Customer feedback event consumption and downstream processing

    @happy-path @IgnoreIfExternalSut
    Scenario: Consuming customer feedback event should trigger downstream processing
        Given a customer feedback received event
        When the event is published to PubSub
        Then the feedback ID should be generated
        And the supplier service should have received the feedback
