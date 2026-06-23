Feature: Pancakes
  Pancake batch creation, mirrored from the C# Pancakes scenarios.

  Scenario: A valid pancake batch is created from its ingredients
    Given a valid pancake request
    When the pancakes are made
    Then a pancake batch is returned with the ingredients

  Scenario: A pancake request without milk is rejected
    Given a pancake request without milk
    When the pancakes are made
    Then the response status is 400
    And the error mentions "'Milk' is required."

  Scenario: Exceeding the topping limit is rejected
    Given a pancake request with six toppings
    When the pancakes are made
    Then the response status is 400
    And the error mentions "Maximum toppings exceeded. Limit is 5."
