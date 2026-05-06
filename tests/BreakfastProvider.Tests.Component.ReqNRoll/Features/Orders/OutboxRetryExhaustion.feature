Feature: Outbox Retry Exhaustion
    /orders - Outbox message transitions to failed after exhausting retries

    @IgnoreIfNeedsEventInfrastructure
    Scenario: An outbox message should transition to failed after exhausting retries
        Given the outbox processor is configured with a failing dispatcher
        And a pancake batch has been created
        And a valid order request for the created batch
        When the order is submitted and retries are exhausted
        Then the outbox message should be in a failed state
