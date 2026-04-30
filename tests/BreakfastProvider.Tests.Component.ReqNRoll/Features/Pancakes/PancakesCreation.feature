Feature: Pancakes Creation
    /pancakes - Creating pancakes with ingredients and optional toppings

    @happy-path
    Scenario: A valid pancake request should return a fresh batch
        Given a valid pancake recipe with all ingredients
        When the pancakes are prepared
        Then the pancakes response should contain a valid batch with all ingredients
        And the cow service should have received a milk request

    Scenario Outline: Pancakes endpoint is called with invalid ingredients should return a bad request response
        Given a valid pancake request with "<Field>" set to "<Value>"
        When the invalid pancake request is submitted
        Then the response should contain error "<Error Message>" with status "<Response Status>"

        Examples:
            | Field | Value                   | Reason            | Error Message                                    | Response Status |
            | Milk  |                         | Milk is required  | 'Milk' is required.                              | Bad Request     |
            | Flour |                         | Flour is required | 'Flour' is required.                             | Bad Request     |
            | Eggs  |                         | Eggs is required  | 'Eggs' is required.                              | Bad Request     |
            | Milk  | <script>alert</script>  | XSS in milk       | Milk contains potentially dangerous content.     | Bad Request     |
            | Flour | <img onerror=x>         | XSS in flour      | Flour contains potentially dangerous content.    | Bad Request     |
            | Eggs  | javascript:void(0)      | XSS in eggs       | Eggs contains potentially dangerous content.     | Bad Request     |

    @IgnoreIfExternalSut
    Scenario: A pancake request with more toppings than allowed should return a bad request response
        Given the max toppings per item is the configured limit
        And a valid pancake recipe with all ingredients
        And the request has more toppings than the configured limit
        When the pancakes are prepared
        Then the pancakes response should indicate too many toppings
