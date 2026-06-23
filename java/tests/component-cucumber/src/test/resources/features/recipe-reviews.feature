Feature: Recipe Reviews
  Recipe review capture (MongoDB), mirrored from the C# RecipeReviews scenarios.

  Scenario: A recipe review is submitted
    When a review for "Classic Pancakes" with rating 5 is submitted
    Then the review is stored with rating 5

  Scenario: A review with an out-of-range rating is rejected
    When a review for "Classic Pancakes" with rating 7 is submitted
    Then the response status is 400
    And the error mentions "'Rating' must be between 1 and 5."
