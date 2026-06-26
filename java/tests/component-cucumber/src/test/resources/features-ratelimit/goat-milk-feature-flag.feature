Feature: Goat milk feature flag
  The goat-milk feature flag, mirrored from the C# Ingredients Goat_Milk_Feature_Flag scenario.
  Runs in the isolated override context with the flag disabled.

  Scenario: The goat-milk endpoint returns not found when the feature is disabled
    When goat milk is requested in the override context
    Then the override response status is 404
