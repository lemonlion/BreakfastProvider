Feature: Recipe Reviews
  Recipe review capture (MongoDB), mirrored from the C# RecipeReviews scenarios.

  Scenario: A recipe review is submitted
    When a review for "Classic Pancakes" with rating 5 is submitted
    Then the review is stored with rating 5

  Scenario: A review with an out-of-range rating is rejected
    When a review for "Classic Pancakes" with rating 7 is submitted
    Then the response status is 400
    And the error mentions "'Rating' must be between 1 and 5."

  Scenario: An existing review is retrievable by id
    When a review is submitted and retrieved by id
    Then the retrieved review matches the submitted review

  Scenario: Reviews are listed by recipe
    When a review is submitted and listed by recipe
    Then the recipe review list contains the submitted review

  Scenario: Retrieving a non-existent review returns not found
    When a non-existent review is retrieved
    Then the response status is 404

  Scenario: A review with a missing recipe name is rejected
    When a review with a missing recipe name is submitted
    Then the response status is 400
    And the error mentions "'Recipe Name' must not be empty."
