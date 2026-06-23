Feature: Toppings
  Topping catalogue and management, mirrored from the C# Toppings scenarios.

  Scenario: The topping catalogue lists the available toppings
    When the topping catalogue is requested
    Then the catalogue includes "Maple Syrup"

  Scenario: A new topping is created with an id
    When a topping named "Caramel" in category "Syrup" is added
    Then the created topping has an id

  Scenario: A topping without a name is rejected
    When a topping named "" in category "Syrup" is added
    Then the response status is 400
    And the error mentions "'Name' is required."
