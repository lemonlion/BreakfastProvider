Feature: Inventory
  Inventory item management, mirrored from the C# Inventory scenarios.

  Scenario: An inventory item is added
    When an inventory item "Flour" with quantity "25.5" is added
    Then the inventory item is stored

  Scenario: A negative quantity is rejected
    When an inventory item "Flour" with quantity "-1" is added
    Then the response status is 400
    And the error mentions "'Quantity' must be greater than or equal to zero."

  Scenario: An existing inventory item is retrievable by id
    When an inventory item is added and retrieved by id
    Then the retrieved inventory item matches

  Scenario: All inventory items are listed
    When an inventory item is added and all items are listed
    Then the inventory list contains the item

  Scenario: An inventory item is updated
    When an inventory item is added and its quantity is updated
    Then the response status is 200

  Scenario: An inventory item is deleted
    When an inventory item is added and deleted
    Then the response status is 204

  Scenario: Retrieving a non-existent inventory item returns not found
    When a non-existent inventory item is retrieved
    Then the response status is 404
