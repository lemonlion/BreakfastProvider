Feature: Audit Logs
  Audit-log capture and query, mirrored from the C# AuditLogs scenarios.

  Scenario: Creating an order is recorded in the audit log
    When an order is placed and its audit log is queried
    Then the audit log records the order creation

  Scenario: Audit logs are filterable by entity type
    When an order is placed and audit logs are filtered by entity type
    Then every audit log entry is of type "Order"

  Scenario: Audit logs are filterable by entity id
    When an order is placed and audit logs are filtered by its entity id
    Then every audit log entry is for that order

  Scenario: Filtering by a non-existent entity type returns an empty collection
    When audit logs are filtered by a non-existent entity type
    Then the audit log collection is empty

  Scenario: Audit logs are returned in descending timestamp order
    When two orders are placed and audit logs are queried
    Then the audit logs are in descending timestamp order
