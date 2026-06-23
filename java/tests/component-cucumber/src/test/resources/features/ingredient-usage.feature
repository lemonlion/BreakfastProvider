Feature: Ingredient Usage
  Ingredient usage analytics (BigQuery), mirrored from the C# IngredientUsage scenarios.

  Scenario: Ingredient usage is recorded
    When ingredient usage of "Flour" is recorded
    Then the usage record is created
