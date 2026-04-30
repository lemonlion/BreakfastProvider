Feature: Telemetry
    /orders - Structured logging and telemetry verification

    @happy-path @IgnoreIfExternalSut
    Scenario: Creating an order should emit a structured log entry
        Given the application is configured with an in-memory log capture
        And a pancake batch has been created
        And a valid order request
        When the order is submitted
        Then a structured log entry should have been captured for order creation
