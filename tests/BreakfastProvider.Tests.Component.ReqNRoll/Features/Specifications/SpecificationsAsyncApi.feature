Feature: Specifications Async Api
    /asyncapi/asyncapi.json - Serving the AsyncAPI specification describing event-driven messaging

    @happy-path
    Scenario: The AsyncApi endpoint should return a valid specification
        When the asyncapi endpoint is called
        Then the response should be valid
        And the asyncapi spec is written to disk
