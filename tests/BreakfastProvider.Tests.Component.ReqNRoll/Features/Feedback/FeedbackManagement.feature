Feature: Feedback Management
    /feedback - Submitting and retrieving customer feedback

    @happy-path
    Scenario: Submitting feedback should return the created feedback
        Given a valid feedback request
        When the feedback is submitted
        Then the feedback response should contain the created feedback

    Scenario: Retrieving existing feedback by id should return the feedback
        Given a feedback entry exists
        When the feedback is retrieved by id
        Then the feedback get response should contain the feedback

    Scenario: Listing feedback for an order should return the feedback
        Given a feedback entry exists
        When the feedback is retrieved by order id
        Then the feedback list response should contain the feedback

    Scenario: Retrieving non-existent feedback should return not found
        When a non-existent feedback is retrieved
        Then the feedback get response should indicate not found

    Scenario: Submitting feedback with missing customer name should return bad request
        Given a feedback request with missing customer name
        When the feedback is submitted
        Then the feedback response should indicate bad request

    Scenario: Submitting feedback with invalid rating should return bad request
        Given a feedback request with an invalid rating
        When the feedback is submitted
        Then the feedback response should indicate bad request
