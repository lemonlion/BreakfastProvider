Feature: Ingredient Waste
  Ingredient waste analytics (BigQuery), mirrored from the C# IngredientWaste scenarios.

  Scenario: Ingredient waste is recorded
    When ingredient waste for recipe "Classic Pancakes" is recorded
    Then the waste record is created

  Scenario: Waste is listed by recipe
    When ingredient waste is recorded and listed by recipe
    Then the waste list for that recipe contains the record

  Scenario: A waste record is deleted
    When a waste record is recorded and deleted
    Then the response status is 204

  Scenario: Recording waste with a missing ingredient name is rejected
    When ingredient waste with a missing ingredient name is recorded
    Then the response status is 400
    And the error mentions "'Ingredient Name' must not be empty."

  Scenario: Recording waste with zero quantity is rejected
    When ingredient waste with zero quantity is recorded
    Then the response status is 400
    And the error mentions "'Quantity Wasted' must be greater than zero."

  Scenario: Recording waste with a missing reason is rejected
    When ingredient waste with a missing reason is recorded
    Then the response status is 400
    And the error mentions "'Reason' must not be empty."
