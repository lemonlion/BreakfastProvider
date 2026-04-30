Feature: Goat Milk Downstream Failure
    /goat-milk - Handling downstream Goat Service failures

    @SkipUnlessFakesControllable
    Scenario: Requesting goat milk when the goat service is unavailable should return a bad gateway response
        Given the goat service will return service unavailable
        When goat milk is requested
        Then the goat milk response should indicate a bad gateway

    @SkipUnlessFakesControllable
    Scenario: Requesting goat milk when the goat service returns an invalid response should return a bad gateway response
        Given the goat service will return an invalid response
        When goat milk is requested
        Then the goat milk response should indicate a bad gateway
