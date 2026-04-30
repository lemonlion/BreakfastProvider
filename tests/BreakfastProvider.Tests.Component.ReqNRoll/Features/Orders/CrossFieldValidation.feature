Feature: Order Cross-Field Validation
    /orders - Cross-field validation with configurable item limits

    @IgnoreIfExternalSut
    Scenario: An order exceeding the maximum items per order should be rejected
        Given the maximum items per order is configured to two
        And a pancake batch has been created
        And an order request with three items
        When the order is submitted
        Then the response should indicate a validation error
        And the error message should reference the item limit

    @IgnoreIfExternalSut
    Scenario: An order at the maximum items per order should be accepted
        Given the maximum items per order is configured to two
        And a pancake batch has been created
        And an order request with two items
        When the order is submitted
        Then the response should indicate success
