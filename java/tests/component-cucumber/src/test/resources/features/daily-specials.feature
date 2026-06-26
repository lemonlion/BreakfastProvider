Feature: Daily Specials
  Daily special listing and ordering, mirrored from the C# DailySpecials scenarios.

  Scenario: The daily specials are listed
    When the daily specials are requested
    Then the specials list includes "Matcha Waffles"

  Scenario: A daily special is ordered
    When a daily special is ordered
    Then the daily special order is confirmed

  Scenario: Ordering a non-existent daily special returns not found
    When an unknown daily special is ordered
    Then the response status is 404

  Scenario: Ordering beyond the threshold returns a conflict
    When the daily special is ordered beyond its limit
    Then the response status is 409

  Scenario: A daily special order with an invalid field is rejected
    When a daily special is ordered with zero quantity
    Then the response status is 400
    And the error mentions "Quantity must be greater than zero."

  Scenario: The same order with the same idempotency key returns the same confirmation
    When a daily special is ordered twice with the same idempotency key
    Then both confirmations are identical

  Scenario: The same order with different idempotency keys returns different confirmations
    When a daily special is ordered twice with different idempotency keys
    Then the two confirmations differ

  Scenario: The remaining quantity decreases after each order
    When the lemon ricotta special is ordered once
    Then the lemon ricotta special has one fewer remaining
