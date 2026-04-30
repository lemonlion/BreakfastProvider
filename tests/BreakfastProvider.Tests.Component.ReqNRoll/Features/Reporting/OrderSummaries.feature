Feature: Order Summaries
    /graphql - Querying order summary reports via GraphQL

    @happy-path @IgnoreIfNeedsDirectDbAccess
    Scenario: Order summaries should contain ingested order data
        Given an order has been created and ingested into the reporting database
        When the order summaries are queried via graphql
        Then the graphql response should contain the ingested order summary

    Scenario: Order summaries should return an empty list when no orders exist
        When the order summaries are queried via graphql
        Then the graphql response should be successful
        And the order summaries list should be empty or not contain the test order
