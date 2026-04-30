Feature: Audit Log Retrieval
    /audit-logs - Retrieving audit log entries for order operations

    @happy-path
    Scenario: Creating an order should produce a retrievable audit log entry
        Given a pancake batch has been created
        And an order has been created for the batch
        When the audit logs are retrieved
        Then the audit log response should contain the order creation entry
        And the cow service should have received a milk request
        And the kitchen service should have received a preparation request
