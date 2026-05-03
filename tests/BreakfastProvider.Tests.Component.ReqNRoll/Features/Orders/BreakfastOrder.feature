Feature: Breakfast Order
    /orders - Creating and managing breakfast orders with event publishing

    @happy-path
    Scenario: A valid order should be created and an event published
        Given a pancake batch has been created
        And a valid order request for the created batch
        When the breakfast order is placed
        Then the order response should contain a complete order
        And an order created event should have been published
        And the kitchen service should have received a preparation request

    Scenario: Creating an order should produce an audit log entry and events
        Given a pancake batch has been created
        And a valid order request for the created batch
        When the breakfast order is placed
        Then the order response should contain a complete order
        And an order created event should have been published
        And a recipe log should have been published to kafka

    Scenario: Creating an order should write an outbox message that gets processed
        Given a pancake batch has been created
        And a valid order request for the created batch
        When the breakfast order is placed
        Then the order response should contain a complete order
        And an outbox message should have been written for the order created event
        And the outbox message should have been processed
