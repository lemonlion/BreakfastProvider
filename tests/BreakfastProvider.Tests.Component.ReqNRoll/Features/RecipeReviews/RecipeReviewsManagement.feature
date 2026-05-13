Feature: Recipe Reviews Management
    /recipe-reviews - Submitting and retrieving recipe reviews (MongoDB)

    @happy-path
    Scenario: Submitting a recipe review should return the created review
        Given a valid recipe review request
        When the recipe review is submitted
        Then the recipe review response should contain the created review

    Scenario: Retrieving existing review by id should return the review
        Given a recipe review has been created
        When the review is retrieved by id
        Then the get response should contain the review

    Scenario: Listing reviews by recipe should return matching reviews
        Given a recipe review has been created
        When the reviews are listed by recipe name
        Then the list response should contain the review

    Scenario: Retrieving a non-existent review should return not found
        When a non-existent review is retrieved
        Then the review get response should indicate not found

    Scenario: Submitting a review with missing recipe name should return bad request
        Given a recipe review request with a missing recipe name
        When the recipe review is submitted
        Then the review post response should indicate bad request

    Scenario: Submitting a review with an invalid rating should return bad request
        Given a recipe review request with an invalid rating
        When the recipe review is submitted
        Then the review post response should indicate bad request
