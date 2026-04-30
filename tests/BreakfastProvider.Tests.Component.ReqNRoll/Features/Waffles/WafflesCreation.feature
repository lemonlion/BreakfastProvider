Feature: Waffles Creation
    /waffles - Creating waffles with ingredients including butter

    @happy-path
    Scenario: A valid waffle request should return a fresh batch
        Given a valid waffle recipe with all ingredients
        When the waffles are prepared
        Then the waffles response should contain a valid batch with all ingredients
        And the cow service should have received a milk request

    Scenario Outline: Waffles endpoint is called with invalid ingredients should return a bad request response
        Given a valid waffle request with "<Field>" set to "<Value>"
        When the invalid waffle request is submitted
        Then the waffle response should contain error "<Error Message>" with status "<Response Status>"

        Examples:
            | Field  | Value                   | Reason             | Error Message                                     | Response Status |
            | Milk   |                         | Milk is required   | 'Milk' is required.                               | Bad Request     |
            | Flour  |                         | Flour is required  | 'Flour' is required.                              | Bad Request     |
            | Eggs   |                         | Eggs is required   | 'Eggs' is required.                               | Bad Request     |
            | Butter |                         | Butter is required | 'Butter' is required.                             | Bad Request     |
            | Milk   | <script>alert</script>  | XSS in milk        | Milk contains potentially dangerous content.      | Bad Request     |
            | Flour  | <img onerror=x>         | XSS in flour       | Flour contains potentially dangerous content.     | Bad Request     |
            | Eggs   | javascript:void(0)      | XSS in eggs        | Eggs contains potentially dangerous content.      | Bad Request     |
            | Butter | <script>alert</script>  | XSS in butter      | Butter contains potentially dangerous content.    | Bad Request     |

    @IgnoreIfExternalSut
    Scenario: A waffle request with more toppings than allowed should return a bad request response
        Given the max toppings per item is the configured limit
        And a valid waffle recipe with all ingredients
        And the waffle request has more toppings than the configured limit
        When the waffles are prepared
        Then the waffles response should indicate too many toppings
