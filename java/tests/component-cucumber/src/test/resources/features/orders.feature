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

  Scenario: An order moves through its complete lifecycle to Completed
    Given a placed breakfast order
    When the order status is updated to "Preparing"
    Then the response status is 200
    When the order status is updated to "Ready"
    Then the response status is 200
    When the order status is updated to "Completed"
    Then the response status is 200
    And the order status is "Completed"

  Scenario: An order is still created when the kitchen service fails
    Given the kitchen service is failing
    And a valid breakfast order
    When the order is placed
    Then the order is created successfully

  Scenario: Orders are returned with pagination metadata
    Given two breakfast orders have been placed
    When orders are listed with page 1 and page size 1
    Then the response status is 200
    And the pagination metadata reflects page 1 with page size 1

  Scenario: An order exceeding the item limit is rejected
    Given an order request with 11 items
    When the order is placed
    Then the response status is 400
    And the error mentions "cannot contain more than 10 items"

  Scenario: A created order can be cancelled
    Given a placed breakfast order
    When the order status is updated to "Cancelled"
    Then the response status is 200
    And the order status is "Cancelled"

  Scenario: An order at the maximum item limit is accepted
    Given an order request with 10 items
    When the order is placed
    Then the order is created successfully

  Scenario: The second page of orders returns different results
    Given two breakfast orders have been placed
    When orders are listed with page 2 and page size 1
    Then the response status is 200
    And the pagination metadata reflects page 2 with page size 1

  Scenario: An order without items is rejected
    Given an order request with 0 items
    When the order is placed
    Then the response status is 400
    And the error mentions "The Items field is required."

  Scenario: Creating an order writes a Created audit log entry
    Given a valid breakfast order
    When the order is placed
    Then a Created audit log entry exists for the order

  Scenario: A previously created order is retrievable by id
    Given a placed breakfast order
    When the placed order is retrieved by id
    Then the retrieved order matches the placed order

  Scenario: A small page size limits the number of results
    Given two breakfast orders have been placed
    When orders are listed with page 1 and page size 1
    Then the response status is 200
    And the page contains 1 item

  Scenario: Listing a page beyond the data returns an empty page
    When orders are listed with page 999999 and page size 10
    Then the page of orders is empty

  Scenario: Updating the status of a non-existent order returns not found
    When the status of a missing order is updated to "Preparing"
    Then the response status is 404

  Scenario: A status update with an invalid field is rejected
    Given a placed breakfast order
    When the order status is updated to ""
    Then the response status is 400
    And the error mentions "'Status' is required."
