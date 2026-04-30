Feature: OpenAPI Specification
    /swagger/v1/swagger.json - Serving the OpenAPI specification describing all REST endpoints

    @happy-path
    Scenario: The OpenApi endpoint should return a valid specification
        When the open api endpoint is called
        Then the response should be valid
        And the response should contain all the endpoints
        And the openapi spec is written to disk
