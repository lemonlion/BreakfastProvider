Feature: Outbox Retry Exhaustion
    /orders - Outbox message transitions to failed after exhausting retries

    @IgnoreIfNeedsEventInfrastructure
    Scenario: An outbox message should transition to failed after exhausting retries
        Given the outbox processor is configured with a failing dispatcher
        And a pending outbox message with a test-specific destination
        Then the outbox message should be in a failed state
