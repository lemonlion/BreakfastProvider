Feature: Audit Logs
  Audit-log capture and query, mirrored from the C# AuditLogs scenarios.

  Scenario: Creating an order is recorded in the audit log
    When an order is placed and its audit log is queried
    Then the audit log records the order creation
