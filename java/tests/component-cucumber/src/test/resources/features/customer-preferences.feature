Feature: Customer Preferences
  Customer preference upsert (Spanner), mirrored from the C# CustomerPreferences scenarios.

  Scenario: A customer preference is saved
    When a preference for "Alice" preferring "oat" milk is saved
    Then the saved preference uses "oat" milk

  Scenario: Existing customer preferences are retrievable
    When a preference is saved and retrieved by id
    Then the retrieved preference is for "Alice"

  Scenario: Customer preferences are updated
    When a saved preference is updated to "almond" milk
    Then the saved preference uses "almond" milk

  Scenario: Retrieving non-existent customer preferences returns not found
    When a non-existent customer preference is retrieved
    Then the response status is 404

  Scenario: Saving preferences with a missing customer name is rejected
    When a preference with a missing customer name is saved
    Then the response status is 400
    And the error mentions "'Customer Name' must not be empty."
