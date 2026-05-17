Feature: Reporting EventGrid Webhook
    /webhooks/eventgrid - Ingredient shipments recorded via EventGrid webhook events

    @happy-path
    Scenario: Ingredient shipments should be recorded when delivered via eventgrid webhook
        Given an ingredient delivery event has been received via eventgrid webhook
        When the ingredient shipments are queried via graphql
        Then the graphql response should contain the ingredient shipment record
