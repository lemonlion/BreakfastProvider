Feature: Degraded Health Check
    /health - Health check endpoint reporting degraded status when downstream services are unavailable

    @SkipUnlessFakesControllable
    Scenario: Health check should report degraded when the cow service is unavailable
        Given the cow service is configured to be unreachable
        When the health check endpoint is called
        Then the health check response should indicate a degraded status
        And the cow service dependency should report degraded

    @SkipUnlessFakesControllable
    Scenario: Health check should report degraded when multiple downstream services are unavailable
        Given the cow service is configured to be unreachable
        And the supplier service is configured to be unreachable
        When the health check endpoint is called
        Then the health check response should indicate a degraded status
        And the cow service dependency should report degraded
        And the supplier service dependency should report degraded
