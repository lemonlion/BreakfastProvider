Feature: Inventory
  Inventory item management, mirrored from the C# Inventory scenarios.

  Scenario: An inventory item is added
    When an inventory item "Flour" with quantity "25.5" is added
    Then the inventory item is stored

  Scenario: A negative quantity is rejected
    When an inventory item "Flour" with quantity "-1" is added
    Then the response status is 400
    And the error mentions "'Quantity' must be greater than or equal to zero."
