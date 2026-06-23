Feature: Reservations
  Table reservation lifecycle, mirrored from the C# Reservations scenarios.

  Scenario: A reservation is created and confirmed
    When a reservation for "Alice" is created
    Then the reservation is confirmed

  Scenario: Cancelling an already-cancelled reservation is a conflict
    Given a confirmed reservation
    When the reservation is cancelled again
    Then the response status is 409
