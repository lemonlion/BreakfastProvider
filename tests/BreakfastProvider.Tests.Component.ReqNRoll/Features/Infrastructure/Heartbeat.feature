Feature: Heartbeat
    /heartbeat - Heartbeat endpoint confirming the service is running

    @happy-path
    Scenario: The heartbeat endpoint should return a running message
        When the heartbeat endpoint is called
        Then the heartbeat response should indicate the service is running
