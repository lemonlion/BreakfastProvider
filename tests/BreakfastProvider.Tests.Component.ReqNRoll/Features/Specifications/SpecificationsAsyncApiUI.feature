Feature: Specifications Async Api UI
    /asyncapi - Serving the AsyncAPI documentation UI

    @happy-path
    Scenario: The AsyncApi UI endpoint should return a valid page
        When the asyncapi ui endpoint is called
        Then the response should be a valid asyncapi page
