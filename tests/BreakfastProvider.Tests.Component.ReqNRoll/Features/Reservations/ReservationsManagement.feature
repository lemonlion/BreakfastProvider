Feature: Reservations Management
    /reservations - Managing table reservations with full CRUD operations and cancellation

    @happy-path
    Scenario: Creating a reservation should return the confirmed reservation
        Given a valid reservation request
        When the reservation is submitted
        Then the reservation response should contain the confirmed booking

    Scenario: Retrieving an existing reservation should return the reservation
        Given a reservation exists
        When the reservation is retrieved by id
        Then the reservation get response should contain the reservation

    Scenario: Cancelling a reservation should return the cancelled reservation
        Given a reservation exists
        When the reservation is cancelled
        Then the cancellation response should indicate the reservation is cancelled

    Scenario: Cancelling an already cancelled reservation should return a conflict response
        Given a cancelled reservation exists
        When the reservation is cancelled again
        Then the cancellation response should indicate a conflict

    Scenario: Deleting a reservation should return no content
        Given a reservation exists
        When the reservation is deleted
        Then the reservation delete response should indicate no content
