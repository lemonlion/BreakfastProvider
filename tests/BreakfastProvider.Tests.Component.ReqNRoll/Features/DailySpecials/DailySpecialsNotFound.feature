Feature: Daily Specials Not Found
    /daily-specials/orders - Ordering a non-existent daily special

    Scenario: Ordering a non-existent daily special should return not found
        Given a daily special order request for a non-existent special
        When the daily special order is submitted
        Then the daily special response should indicate not found
