Feature: Health Check Detail
    /health - Health check response includes detailed entry descriptions and data

    Scenario: The health check response should include description and data for each entry
        When the health check endpoint is called
        Then the health check response should contain detailed entries
