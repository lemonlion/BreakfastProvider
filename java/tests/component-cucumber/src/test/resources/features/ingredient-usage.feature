Feature: Ingredient Usage
  Ingredient usage analytics (BigQuery), mirrored from the C# IngredientUsage scenarios.

  Scenario: Ingredient usage is recorded
    When ingredient usage of "Flour" is recorded
    Then the usage record is created

  Scenario: Usage is listed by ingredient
    When ingredient usage is recorded and listed by ingredient
    Then the usage list for that ingredient contains the record

  Scenario: The usage summary is available
    When the ingredient usage summary is requested
    Then the response status is 200

  Scenario: Recording usage with zero quantity is rejected
    When ingredient usage with zero quantity is recorded
    Then the response status is 400
    And the error mentions "'Quantity Used' must be greater than zero."

  Scenario: Recording usage with a missing ingredient name is rejected
    When ingredient usage with a missing ingredient name is recorded
    Then the response status is 400
    And the error mentions "'Ingredient Name' must not be empty."
