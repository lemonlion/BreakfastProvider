Feature: Audit Log Filtering
    /audit-logs - Filtering audit logs by entity type and entity ID

    @IgnoreIfNeedsEventInfrastructure
    Scenario: Audit logs should be filterable by entity type
        Given a pancake batch has been created
        And an order has been created for the batch
        When audit logs are requested filtered by entity type
        Then the audit log response should only contain order entries

    @IgnoreIfNeedsEventInfrastructure
    Scenario: Audit logs should be filterable by entity id
        Given a pancake batch has been created
        And an order has been created for the batch
        When audit logs are requested filtered by entity id
        Then the audit log response should contain the specific order entry

    Scenario: Filtering audit logs by a non-existent entity type should return an empty collection
        When audit logs are requested filtered by a non-existent entity type
        Then the audit log response should be an empty collection

    @IgnoreIfNeedsEventInfrastructure
    Scenario: Audit logs should be returned in descending timestamp order
        Given a pancake batch has been created
        And an order has been created for the batch
        When audit logs are requested filtered by entity type
        Then the audit logs should be ordered by timestamp descending
