Feature: Daily Specials Validation
    /daily-specials/orders - Input validation for daily special orders

    Scenario Outline: Daily specials order endpoint is called with invalid fields should return a bad request response
        Given a valid daily special order request with "<Field>" set to "<Value>"
        When the invalid daily special order request is submitted
        Then the daily special response should contain error "<Error Message>" with status "<Response Status>"

        Examples:
            | Field     | Value | Reason                  | Error Message                        | Response Status |
            | SpecialId |       | SpecialId is required   | 'Special Id' is required.            | Bad Request     |
            | Quantity  | 0     | Quantity must be > zero | Quantity must be greater than zero.  | Bad Request     |
