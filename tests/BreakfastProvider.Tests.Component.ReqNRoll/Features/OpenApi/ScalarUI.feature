Feature: Scalar UI
    /scalar/v1 - Serving the interactive API documentation UI powered by Scalar

    @happy-path
    Scenario: The Scalar UI endpoint should return a valid page
        When the scalar ui endpoint is called
        Then the response should be a valid scalar page
