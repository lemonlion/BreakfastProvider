Feature: Goat Milk Sourcing
    /goat-milk - Retrieving goat milk from the Goat Service

    @happy-path
    Scenario: A valid goat milk request should return fresh goat milk
        When goat milk is requested
        Then the goat milk response should contain fresh goat milk
