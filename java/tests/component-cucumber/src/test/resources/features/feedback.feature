Feature: Feedback
  Customer feedback capture (Spanner), mirrored from the C# Feedback scenarios.

  Scenario: Feedback is submitted with a valid rating
    When feedback with rating 5 is submitted
    Then the feedback is stored with rating 5

  Scenario: Feedback with an out-of-range rating is rejected
    When feedback with rating 9 is submitted
    Then the response status is 400
    And the error mentions "'Rating' must be between 1 and 5."

  Scenario: Existing feedback is retrievable by id
    When valid feedback is submitted and retrieved by id
    Then the retrieved feedback matches the submitted feedback

  Scenario: Feedback for an order is listed
    When valid feedback is submitted and listed for its order
    Then the order feedback list contains the submitted feedback

  Scenario: Retrieving non-existent feedback returns not found
    When a non-existent feedback is retrieved
    Then the response status is 404

  Scenario: Feedback with a missing customer name is rejected
    When feedback with a missing customer name is submitted
    Then the response status is 400
    And the error mentions "'Customer Name' must not be empty."
