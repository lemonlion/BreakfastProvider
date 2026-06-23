Feature: Feedback
  Customer feedback capture (Spanner), mirrored from the C# Feedback scenarios.

  Scenario: Feedback is submitted with a valid rating
    When feedback with rating 5 is submitted
    Then the feedback is stored with rating 5

  Scenario: Feedback with an out-of-range rating is rejected
    When feedback with rating 9 is submitted
    Then the response status is 400
    And the error mentions "'Rating' must be between 1 and 5."
