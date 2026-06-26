Feature: Chef Notes
  Chef note capture (MongoDB), mirrored from the C# ChefNotes scenarios.

  Scenario: A chef note is recorded for a recipe
    When a chef note for recipe "Classic Pancakes" is recorded
    Then the chef note is stored with an id

  Scenario: A recorded chef note is retrievable by id
    When a chef note for recipe "Classic Pancakes" is recorded
    And the recorded chef note is retrieved
    Then the retrieved chef note has chef "Chef Remy"

  Scenario: A chef note can be updated
    When a chef note for recipe "Classic Pancakes" is recorded
    And the recorded chef note is updated
    Then the response status is 200

  Scenario: Retrieving a non-existent chef note returns not found
    When a non-existent chef note is retrieved
    Then the response status is 404

  Scenario: Updating a non-existent chef note returns not found
    When a non-existent chef note is updated
    Then the response status is 404

  Scenario: Chef notes are listed by recipe
    When a chef note for recipe "ListRecipeAlpha" is recorded
    And chef notes for recipe "ListRecipeAlpha" are listed
    Then the listed chef notes include the recorded note

  Scenario: A chef note without a recipe name is rejected
    When a chef note for recipe "" is recorded
    Then the response status is 400
    And the error mentions "'Recipe Name' must not be empty."

  Scenario: A chef note without note text is rejected
    When a chef note without note text is recorded
    Then the response status is 400
    And the error mentions "'Note Text' must not be empty."
