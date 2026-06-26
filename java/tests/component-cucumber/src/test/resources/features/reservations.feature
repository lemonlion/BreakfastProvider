Feature: Reservations
  Table reservation lifecycle, mirrored from the C# Reservations scenarios.

  Scenario: A reservation is created and confirmed
    When a reservation for "Alice" is created
    Then the reservation is confirmed

  Scenario: An existing reservation is retrievable
    Given a confirmed reservation
    When the reservation is retrieved
    Then the retrieved reservation is for "Alice"

  Scenario: A reservation is cancelled
    Given a confirmed reservation
    When the reservation is cancelled
    Then the reservation status is "Cancelled"

  Scenario: Cancelling an already-cancelled reservation is a conflict
    Given a confirmed reservation
    When the reservation is cancelled again
    Then the response status is 409

  Scenario: A reservation is deleted
    Given a confirmed reservation
    When the reservation is deleted
    Then the response status is 204
