Feature: Menu
  Menu availability gated by the supplier, mirrored from the C# Menu scenarios.

  Scenario: The menu is available when the supplier confirms ingredients
    Given the supplier confirms ingredient availability
    When the menu is requested
    Then every menu item is available

  Scenario: The menu is unavailable when the supplier is down
    Given the supplier is unavailable
    When the menu is requested
    Then every menu item is unavailable
