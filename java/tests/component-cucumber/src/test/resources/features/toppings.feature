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

  Scenario: Raspberries are included when the feature flag is enabled
    When the topping catalogue is requested
    Then the catalogue includes "Raspberries"

  Scenario: A topping name with HTML or script content is rejected
    When a topping named "<script>alert(1)</script>" in category "Syrup" is added
    Then the response status is 400
    And the error mentions "must not contain HTML or script content."

  Scenario: An existing topping is updated
    When an existing topping is updated
    Then the updated topping is named "Golden Syrup"

  Scenario: Updating a non-existent topping returns not found
    When a non-existent topping is updated
    Then the response status is 404

  Scenario: An existing topping is deleted
    When an existing topping is deleted
    Then the response status is 204

  Scenario: Deleting a non-existent topping returns not found
    When a non-existent topping is deleted
    Then the response status is 404

  Scenario: Updating a topping with HTML or script content is rejected
    When an existing topping is updated with HTML or script content
    Then the response status is 400
    And the error mentions "must not contain HTML or script content."
