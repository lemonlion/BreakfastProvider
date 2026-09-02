Feature: Order Timings Analytics
    /order-timings - Recording and summarising kitchen order timings (ClickHouse)

    @happy-path
    Scenario: Recording an order timing should return the created record
        Given a valid order timing request
        When the order timing is recorded
        Then the timing response should contain the created record

    Scenario: Listing timings by station should return matching records
        Given an order timing record has been created
        When the timings are listed by station
        Then the timing list response should contain the record

    Scenario: Getting the timing summary should return aggregated data
        Given an order timing record has been created
        When the timing summary is requested
        Then the summary should contain aggregated data for the station

    Scenario: Recording a timing with missing station should return bad request
        Given an order timing request with a missing station
        When the order timing is recorded
        Then the timing post response should indicate bad request

    Scenario: Recording a timing with zero prep seconds should return bad request
        Given an order timing request with zero prep seconds
        When the order timing is recorded
        Then the timing post response should indicate bad request
