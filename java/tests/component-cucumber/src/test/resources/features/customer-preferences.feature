Feature: Customer Preferences
  Customer preference upsert (Spanner), mirrored from the C# CustomerPreferences scenarios.

  Scenario: A customer preference is saved
    When a preference for "Alice" preferring "oat" milk is saved
    Then the saved preference uses "oat" milk
