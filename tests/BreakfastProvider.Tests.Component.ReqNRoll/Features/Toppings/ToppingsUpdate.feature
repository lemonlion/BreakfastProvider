Feature: Toppings Update
    /toppings - Updating existing toppings

    @happy-path
    Scenario: Updating an existing topping should return the updated topping
        Given a known blueberry topping exists
        And a valid update topping request
        When the topping is updated
        Then the update response should contain the updated topping

    Scenario: Updating a non-existent topping should return not found
        Given a topping id that does not exist
        And a valid update topping request
        When the topping is updated
        Then the update response should indicate not found

    Scenario Outline: Update toppings endpoint is called with invalid or dangerous input
        Given a known topping exists
        And a valid update topping request with "<Field>" set to "<Value>"
        When the invalid update topping request is submitted
        Then the update response should contain error "<Error Message>" with status "<Response Status>"

        Examples:
            | Field    | Value                          | Reason                 | Error Message                                    | Response Status |
            | Name     | <script>alert('xss')</script>  | Script tag in name     | Name contains potentially dangerous content.     | Bad Request     |
            | Name     | <img src=x onerror=alert(1)>   | Event handler in name  | Name contains potentially dangerous content.     | Bad Request     |
            | Category | <script>alert('xss')</script>  | Script tag in category | Category contains potentially dangerous content. | Bad Request     |
            | Category | javascript:alert(1)            | Javascript protocol    | Category contains potentially dangerous content. | Bad Request     |
            | Name     |                                | Name is required       | 'Name' is required.                              | Bad Request     |
            | Category |                                | Category is required   | 'Category' is required.                          | Bad Request     |
