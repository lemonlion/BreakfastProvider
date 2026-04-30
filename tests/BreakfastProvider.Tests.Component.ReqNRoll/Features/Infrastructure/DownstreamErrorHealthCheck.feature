Feature: Downstream Error Health Check
    /health - Health check reports degraded when a downstream service returns a non-success HTTP status

    @SkipUnlessFakesControllable
    Scenario: Health check should report degraded when a downstream service returns a non-success status
        Given the kitchen service health check is configured to use a failing endpoint
        When the health check endpoint is called
        Then the health check response should indicate a degraded status
        And the kitchen service dependency should report degraded with a status code description
