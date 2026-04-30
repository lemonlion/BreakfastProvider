Feature: Correlation Id
    Cross-cutting - X-Correlation-Id header propagation across API responses

    @happy-path
    Scenario: A request with a correlation id should return the same id in the response
        Given a request with a known correlation id
        When the request is sent to the menu endpoint
        Then the response should contain the same correlation id

    Scenario: A request without a correlation id should have one generated in the response
        When a request without a correlation id is sent to the menu endpoint
        Then the response should contain a generated correlation id
