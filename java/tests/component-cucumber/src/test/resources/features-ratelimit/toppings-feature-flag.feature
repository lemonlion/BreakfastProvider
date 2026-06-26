Feature: Toppings feature flag
  The raspberry topping feature flag, mirrored from the C# Toppings Feature_Flag scenario.
  Runs in the isolated override context with the flag disabled.

  Scenario: Raspberries are excluded when the feature flag is disabled
    When the topping catalogue is requested in the override context
    Then the catalogue excludes "Raspberries"
