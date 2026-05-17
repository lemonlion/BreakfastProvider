Feature: Reporting Batch Completions
    /graphql - Querying batch completion records populated by Pub/Sub consumption

    @happy-path
    Scenario: Batch completions should contain data ingested via pubsub consumer
        Given a pancake batch has been created for batch completions
        When the batch completions are queried via graphql
        Then the graphql response should contain the batch completion record
