Feature: Recipe Summary via gRPC
    /grpc - Retrieving recipe summaries via gRPC

    @happy-path
    Scenario: Pancake recipe summary should return correct data
        When a recipe summary is requested for "Pancakes" via gRPC
        Then the recipe summary should contain 42 total batches
        And the recipe summary should contain ingredients "Milk, Flour, Eggs"

    @happy-path
    Scenario: Waffle recipe summary should return correct data
        When a recipe summary is requested for "Waffles" via gRPC
        Then the recipe summary should contain 28 total batches
        And the recipe summary should contain ingredients "Milk, Flour, Eggs, Butter"

    Scenario: Unknown recipe type should return zero batches
        When a recipe summary is requested for "Unknown" via gRPC
        Then the recipe summary should contain 0 total batches
        And the recipe summary should contain no ingredients
