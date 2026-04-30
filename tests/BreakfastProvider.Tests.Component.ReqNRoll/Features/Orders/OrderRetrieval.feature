Feature: Order Retrieval
    /orders - Retrieving orders by ID

    @happy-path @IgnoreIfNeedsEventInfrastructure
    Scenario: A created order should be retrievable by its ID
        Given an order has been created
        When the order is retrieved by id
        Then the order retrieval response should contain the order

    Scenario: Retrieving a non-existent order should return not found
        When a non-existent order is retrieved
        Then the order retrieval response should indicate not found
