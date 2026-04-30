Feature: Recipe Reports
    /graphql - Querying recipe reports and aggregations via GraphQL

    @happy-path @IgnoreIfNeedsDirectDbAccess
    Scenario: Recipe reports should contain ingested recipe data
        Given recipe logs have been ingested into the reporting database
        When the recipe reports are queried via graphql
        Then the graphql response should contain the ingested recipe reports

    @IgnoreIfNeedsDirectDbAccess
    Scenario: Ingredient usage should aggregate across multiple recipes
        Given multiple recipe logs have been ingested with overlapping ingredients
        When the ingredient usage is queried via graphql
        Then the ingredient usage should reflect aggregated counts

    @IgnoreIfNeedsDirectDbAccess
    Scenario: Popular recipes should return recipe types ordered by frequency
        Given multiple recipe logs of different types have been ingested
        When the popular recipes are queried via graphql
        Then the popular recipes should be ordered by count descending
