Feature: Complete Order Lifecycle
    /orders - Complete order lifecycle from creation through to completion

    @happy-path @IgnoreIfNeedsEventInfrastructure
    Scenario: An order should progress through all status transitions to completion
        Given a pancake batch has been created
        And a breakfast order has been placed for the batch
        When the order progresses through all statuses to completed
        Then the completed order should be retrievable with all details
        And an audit log entry should exist for the order
        And the cow service should have received a milk request
        And the kitchen service should have received a preparation request
