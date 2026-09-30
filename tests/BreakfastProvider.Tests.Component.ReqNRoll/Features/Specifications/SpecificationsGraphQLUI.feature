Feature: Specifications GraphQL UI
    /graphql/ - Serving the Nitro GraphQL IDE

    @happy-path
    Scenario: The GraphQL UI endpoint should return a valid page
        When the graphql ui endpoint is called
        Then the response should be a valid nitro page
        And the page should be served by the service itself
