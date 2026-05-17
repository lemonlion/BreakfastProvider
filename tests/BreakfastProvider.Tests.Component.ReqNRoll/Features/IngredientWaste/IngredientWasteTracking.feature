Feature: Ingredient Waste Tracking
    /ingredient-waste - Recording and managing ingredient waste (BigQuery)

    @happy-path
    Scenario: Recording ingredient waste should return the created record
        Given a valid ingredient waste request
        When the waste is recorded
        Then the waste response should contain the created record

    Scenario: Listing waste by recipe should return matching records
        Given an ingredient waste record has been created
        When the waste is listed by recipe
        Then the waste list response should contain the record

    Scenario: Deleting a waste record should return no content
        Given an ingredient waste record has been created
        When the waste record is deleted
        Then the delete response should indicate no content

    Scenario: Recording waste with missing ingredient name should return bad request
        Given a waste request with a missing ingredient name
        When the waste is recorded
        Then the waste post response should indicate bad request

    Scenario: Recording waste with zero quantity should return bad request
        Given a waste request with zero quantity
        When the waste is recorded
        Then the waste post response should indicate bad request

    Scenario: Recording waste with missing reason should return bad request
        Given a waste request with a missing reason
        When the waste is recorded
        Then the waste post response should indicate bad request
