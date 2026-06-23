Feature: Chef Notes
  Chef note capture (MongoDB), mirrored from the C# ChefNotes scenarios.

  Scenario: A chef note is recorded for a recipe
    When a chef note for recipe "Classic Pancakes" is recorded
    Then the chef note is stored with an id

  Scenario: A chef note without a recipe name is rejected
    When a chef note for recipe "" is recorded
    Then the response status is 400
    And the error mentions "'Recipe Name' must not be empty."
