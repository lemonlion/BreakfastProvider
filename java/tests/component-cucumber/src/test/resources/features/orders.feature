Feature: Orders
  Breakfast order creation and lifecycle, mirrored from the C# Orders scenarios.

  Scenario: A valid order is created and the kitchen is notified
    Given a valid breakfast order
    When the order is placed
    Then the order is created successfully
    And the kitchen receives a preparation request

  Scenario: Retrieving a non-existent order returns not found
    When a missing order is retrieved
    Then the response status is 404

  Scenario: A valid status transition updates the order
    Given a placed breakfast order
    When the order status is updated to "Preparing"
    Then the response status is 200
    And the order status is "Preparing"

  Scenario: An invalid status transition is rejected
    Given a placed breakfast order
    When the order status is updated to "Ready"
    Then the response status is 409

  Scenario: An order without a customer name is rejected
    Given an order request without a customer name
    When the order is placed
    Then the response status is 400
    And the error mentions "'Customer Name' is required."
