Feature: Daily Specials
  Daily special listing and ordering, mirrored from the C# DailySpecials scenarios.

  Scenario: The daily specials are listed
    When the daily specials are requested
    Then the specials list includes "Matcha Waffles"

  Scenario: A daily special is ordered
    When a daily special is ordered
    Then the daily special order is confirmed
