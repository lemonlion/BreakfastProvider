Feature: Daily Specials Idempotency
    /daily-specials/orders - Idempotent order creation using Idempotency-Key header

    Scenario: Submitting the same order with the same idempotency key should return the same confirmation
        Given the cinnamon swirl order count is reset
        And an order request with an idempotency key
        When the order is submitted twice with the same idempotency key
        Then both responses should return the same confirmation id

    Scenario: Submitting the same order with different idempotency keys should return different confirmations
        Given the cinnamon swirl order count is reset
        And an order request for the same special
        When the order is submitted with two different idempotency keys
        Then the responses should have different confirmation ids
