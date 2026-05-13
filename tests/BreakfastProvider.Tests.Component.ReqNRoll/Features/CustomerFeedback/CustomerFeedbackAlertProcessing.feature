Feature: Customer Feedback Alert Processing
    /customer-feedback - Customer feedback alert processing (PubSub → MongoDB → gRPC → HTTP)

    @happy-path
    Scenario: Submitting customer feedback should trigger event consumption and downstream calls
        Given a valid customer feedback request
        When the customer feedback is submitted
        Then the feedback response should be accepted
        And the supplier service should have received the feedback
