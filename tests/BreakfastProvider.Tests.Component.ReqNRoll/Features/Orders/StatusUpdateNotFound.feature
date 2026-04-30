Feature: Status Update Not Found
    /orders - Updating status of a non-existent order

    Scenario: Updating the status of a non-existent order should return not found
        When a status update is sent for a non-existent order
        Then the status update response should indicate not found
