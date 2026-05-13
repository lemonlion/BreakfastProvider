Feature: Ingredient Usage Analytics
    /ingredient-usage - Recording and summarising ingredient usage (BigQuery)

    @happy-path
    Scenario: Recording ingredient usage should return the created record
        Given a valid ingredient usage request
        When the ingredient usage is recorded
        Then the usage response should contain the created record

    Scenario: Listing usage by ingredient should return matching records
        Given an ingredient usage record has been created
        When the usage is listed by ingredient name
        Then the usage list response should contain the record

    Scenario: Getting usage summary should return aggregated data
        Given an ingredient usage record has been created
        When the usage summary is requested
        Then the summary should contain aggregated data

    Scenario: Recording usage with missing ingredient name should return bad request
        Given an ingredient usage request with a missing ingredient name
        When the ingredient usage is recorded
        Then the usage post response should indicate bad request

    Scenario: Recording usage with zero quantity should return bad request
        Given an ingredient usage request with zero quantity
        When the ingredient usage is recorded
        Then the usage post response should indicate bad request
