Feature: Milk Sourcing
  Sourcing milk from the Cow service, mirrored from the C# Ingredients scenarios.

  Scenario: Milk is sourced from the cow service
    When milk is sourced
    Then fresh milk is returned

  Scenario: A cow service failure surfaces as a bad gateway
    Given the cow service is unavailable
    When milk is sourced
    Then the response status is 502
