Feature: Muffins
  Apple-cinnamon muffin batch creation, mirrored from the C# Muffins scenarios.

  Scenario: A valid muffin batch bakes with the requested profile
    Given a valid muffin request
    When the muffins are made
    Then a muffin batch is returned with the baking profile

  Scenario: A baking temperature outside the allowed range is rejected
    Given a muffin request with an out-of-range baking temperature
    When the muffins are made
    Then the response status is 400
    And the error mentions "Baking temperature must be between 150 and 220 degrees."
