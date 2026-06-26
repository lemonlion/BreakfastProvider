Feature: Milk Sourcing
  Sourcing milk from the Cow service, mirrored from the C# Ingredients scenarios.

  Scenario: Milk is sourced from the cow service
    When milk is sourced
    Then fresh milk is returned

  Scenario: A cow service failure surfaces as a bad gateway
    Given the cow service is unavailable
    When milk is sourced
    Then the response status is 502

  Scenario: A cow invalid response surfaces as a bad gateway
    Given the cow service returns an invalid response
    When milk is sourced
    Then the response status is 502

  Scenario: Goat milk is sourced from the goat service
    When goat milk is sourced
    Then fresh goat milk is returned

  Scenario: A goat service failure surfaces as a bad gateway
    Given the goat service is unavailable
    When goat milk is sourced
    Then the response status is 502

  Scenario: A goat invalid response surfaces as a bad gateway
    Given the goat service returns an invalid response
    When goat milk is sourced
    Then the response status is 502

  Scenario: Goat milk is served fresh from the goat service
    When goat milk is sourced
    Then fresh goat milk comes from the goat service

  Scenario: A cow service timeout surfaces as a bad gateway
    Given the cow service is slow to respond
    When milk is sourced
    Then the response status is 502
