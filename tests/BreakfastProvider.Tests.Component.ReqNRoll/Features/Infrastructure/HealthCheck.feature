Feature: Health Check
    /health - Health check endpoint with dependency status for monitoring

    @happy-path
    Scenario: The health check endpoint should return a healthy status with dependency details
        When the health check endpoint is called
        Then the health check response should indicate healthy with all dependencies
