Feature: Equipment Alerts
    /graphql - Querying equipment alerts populated by Event Hub consumption

    @happy-path
    Scenario: Equipment alerts should contain data ingested via Event Hub consumer
        Given a pancake batch has been created
        When the equipment alerts are queried via graphql
        Then the graphql response should contain the equipment alert record
