Feature: Equipment Readings Monitoring
    /equipment-readings - Recording and monitoring kitchen equipment readings (ClickHouse)

    @happy-path
    Scenario: Recording an equipment reading should return the created record
        Given a valid equipment reading request
        When the equipment reading is recorded
        Then the reading response should contain the created record

    Scenario: Listing readings by equipment should return matching records
        Given an equipment reading record has been created
        When the readings are listed by equipment
        Then the reading list response should contain the record

    Scenario: Deleting a reading should remove it from the list
        Given an equipment reading record has been created
        When the reading is deleted
        Then the reading delete response should indicate no content
        And the reading should no longer be listed

    Scenario: Recording a reading with missing metric should return bad request
        Given an equipment reading request with a missing metric
        When the equipment reading is recorded
        Then the reading post response should indicate bad request

    Scenario: Recording a reading with zero value should return bad request
        Given an equipment reading request with zero value
        When the equipment reading is recorded
        Then the reading post response should indicate bad request
