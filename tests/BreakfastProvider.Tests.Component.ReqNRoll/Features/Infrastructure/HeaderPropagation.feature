Feature: Header Propagation
    /milk; /menu - X-Correlation-Id header propagation to downstream services

    @happy-path @SkipUnlessFakesControllable
    Scenario: A request with a correlation id should forward it to the cow service
        Given a request with a known correlation id
        When milk is requested with the correlation id
        Then the cow service should have received the correlation id

    @SkipUnlessFakesControllable
    Scenario: A request with a correlation id should forward it to the supplier service
        Given a request with a known correlation id
        And the menu cache is cleared
        When the menu is requested with the correlation id
        Then the supplier service should have received the correlation id
