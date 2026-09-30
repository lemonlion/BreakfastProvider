Feature: Specifications GraphQL Schema
    /graphql/schema.json; /graphql/schema.graphql - Serving the GraphQL schema as introspection JSON and as a schema definition

    @happy-path
    Scenario: The GraphQL schema endpoint should return a valid specification
        When the graphql schema endpoint is called
        Then the response should be valid
        And the schema should contain all the reporting queries
        And the reporting queries should be documented
        And the graphql schema is written to disk

    @happy-path
    Scenario: The GraphQL schema definition endpoint should return the schema definition
        When the graphql schema definition endpoint is called
        Then the response should be a graphql schema definition
        And the schema definition should declare all the reporting queries
        And the graphql schema definition is written to disk
