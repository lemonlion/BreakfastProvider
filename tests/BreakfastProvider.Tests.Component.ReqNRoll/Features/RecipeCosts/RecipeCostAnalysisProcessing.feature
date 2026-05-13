Feature: Recipe Cost Analysis Processing
    /recipe-costs - Recipe cost analysis processing (Kafka → BigQuery → gRPC → HTTP)

    @happy-path
    Scenario: Submitting recipe cost should trigger event consumption and downstream calls
        Given a valid recipe cost calculation request
        When the recipe cost calculation is submitted
        Then the cost response should be accepted
        And the kitchen service should have received the preparation request
