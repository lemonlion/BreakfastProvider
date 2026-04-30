Feature: Order Validation
    /orders - Input validation for order creation and status updates

    Scenario Outline: Orders endpoint is called with invalid fields should return a bad request response
        Given a valid order request with "<Field>" set to "<Value>"
        When the invalid order request is submitted
        Then the order response should contain error "<Error Message>" with status "<Response Status>"

        Examples:
            | Field             | Value | Reason                             | Error Message                       | Response Status |
            | CustomerName      |       | Customer name is required          | 'Customer Name' is required.        | Bad Request     |
            | Items             |       | At least one item is required      | The Items field is required.        | Bad Request     |
            | Items[0].ItemType |       | Item type is required              | 'Item Type' is required.            | Bad Request     |
            | Items[0].BatchId  |       | Batch ID is required               | 'Batch Id' is required.             | Bad Request     |
            | Items[0].Quantity | 0     | Quantity must be greater than zero | Quantity must be greater than zero. | Bad Request     |

    Scenario Outline: Order status update endpoint is called with invalid fields should return a bad request response
        Given a valid status update request with "<Field>" set to "<Value>"
        When the invalid status update request is submitted
        Then the status update response should contain error "<Error Message>" with status "<Response Status>"

        Examples:
            | Field  | Value | Reason             | Error Message            | Response Status |
            | Status |       | Status is required | 'Status' is required.    | Bad Request     |
