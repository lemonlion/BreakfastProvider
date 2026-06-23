Feature: Waffles
  Waffle batch creation, mirrored from the C# Waffles scenarios.

  Scenario: A valid waffle batch includes butter among the ingredients
    Given a valid waffle request
    When the waffles are made
    Then a waffle batch is returned with butter

  Scenario: A waffle request without butter is rejected
    Given a waffle request without butter
    When the waffles are made
    Then the response status is 400
    And the error mentions "'Butter' is required."
