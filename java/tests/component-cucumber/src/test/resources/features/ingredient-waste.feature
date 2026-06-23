Feature: Ingredient Waste
  Ingredient waste analytics (BigQuery), mirrored from the C# IngredientWaste scenarios.

  Scenario: Ingredient waste is recorded
    When ingredient waste for recipe "Classic Pancakes" is recorded
    Then the waste record is created
