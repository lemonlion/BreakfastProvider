Feature: Infrastructure
  Cross-cutting endpoints mirrored from the C# Infrastructure scenarios: heartbeat, health checks
  and correlation-id propagation.

  Scenario: Heartbeat reports the service is running
    When the heartbeat endpoint is called
    Then the heartbeat status is "ok"

  Scenario: Health check reports healthy with all dependencies
    When the health check endpoint is called
    Then the overall health status is "Healthy"
    And the health results include all dependency checks

  Scenario: Health check response contains detailed entries
    When the health check endpoint is called
    Then each health entry has a status and a data object
    And each downstream health entry has a description

  Scenario: A known correlation id is echoed back
    When the menu is requested with correlation id "11111111-2222-3333-4444-555555555555"
    Then the response echoes correlation id "11111111-2222-3333-4444-555555555555"

  Scenario: A correlation id is generated when none is supplied
    When the menu is requested without a correlation id
    Then the response contains a generated correlation id

  Scenario: A correlation id is propagated to downstream services
    When milk is requested with correlation id "99999999-8888-7777-6666-555555555555"
    Then the cow service received correlation id "99999999-8888-7777-6666-555555555555"
