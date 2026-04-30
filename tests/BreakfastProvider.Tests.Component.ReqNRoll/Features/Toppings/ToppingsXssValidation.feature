Feature: Toppings XSS Validation
    /toppings - XSS and input validation for toppings

    Scenario Outline: Toppings endpoint is called with invalid or dangerous input
        Given a valid topping request with "<Field>" set to "<Value>"
        When the invalid topping request is submitted
        Then the topping response should contain error "<Error Message>" with status "<Response Status>"

        Examples:
            | Field    | Value                          | Reason                 | Error Message                                    | Response Status |
            | Name     | <script>alert('xss')</script>  | Script tag in name     | Name contains potentially dangerous content.     | Bad Request     |
            | Name     | <img src=x onerror=alert(1)>   | Event handler in name  | Name contains potentially dangerous content.     | Bad Request     |
            | Category | <script>alert('xss')</script>  | Script tag in category | Category contains potentially dangerous content. | Bad Request     |
            | Category | javascript:alert(1)            | Javascript protocol    | Category contains potentially dangerous content. | Bad Request     |
            | Name     |                                | Name is required       | 'Name' is required.                              | Bad Request     |
            | Category |                                | Category is required   | 'Category' is required.                          | Bad Request     |
