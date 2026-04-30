Feature: Order Status Transition
    /orders - Order status transitions following the order lifecycle

    @IgnoreIfNeedsEventInfrastructure
    Scenario Outline: A valid status transition should update the order
        Given an order exists with status "<FromStatus>"
        When the order status is updated to "<ToStatus>"
        Then the order status should be updated successfully to "<ToStatus>"

        Examples:
            | FromStatus | ToStatus   |
            | Created    | Preparing  |
            | Created    | Cancelled  |
            | Preparing  | Ready      |
            | Ready      | Completed  |

    @IgnoreIfNeedsEventInfrastructure
    Scenario Outline: An invalid status transition should return a conflict response
        Given an order exists with status "<FromStatus>"
        When the order status is updated to "<ToStatus>"
        Then the response should indicate an invalid state transition

        Examples:
            | FromStatus | ToStatus   |
            | Created    | Ready      |
            | Created    | Completed  |
            | Preparing  | Cancelled  |
            | Ready      | Preparing  |
            | Completed  | Preparing  |
            | Cancelled  | Preparing  |
            | Cancelled  | Ready      |
