Feature: Order Rate Limiting
    /orders - Rate limiting on order creation

    @IgnoreIfExternalSut
    Scenario: Exceeding the rate limit should return too many requests
        Given the rate limit is configured to allow one request per window
        And a pancake batch has been created
        And a valid order request
        When the order is submitted twice in rapid succession
        Then the first request should succeed
        And the second request should be rate limited
