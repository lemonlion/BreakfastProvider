Feature: Milk Downstream Failure
    /milk - Handling downstream Cow Service failures

    @SkipUnlessFakesControllable
    Scenario: Requesting milk when the cow service is unavailable should return a bad gateway response
        Given the cow service will return service unavailable
        When milk is requested
        Then the milk response should indicate a bad gateway

    @SkipUnlessFakesControllable
    Scenario: Requesting milk when the cow service times out should return a bad gateway response
        Given the cow service will return a timeout
        When milk is requested
        Then the milk response should indicate a bad gateway

    @SkipUnlessFakesControllable
    Scenario: Requesting milk when the cow service returns an invalid response should return a bad gateway response
        Given the cow service will return an invalid response
        When milk is requested
        Then the milk response should indicate a bad gateway
