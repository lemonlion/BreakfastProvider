Feature: Staff Management
    /staff - Managing kitchen staff members with full CRUD operations

    @happy-path
    Scenario: Adding a new staff member should return the created member
        Given a valid staff member request
        When the staff member is submitted
        Then the staff response should contain the created member

    Scenario: Retrieving an existing staff member should return the member
        Given a staff member exists
        When the staff member is retrieved by id
        Then the staff get response should contain the member

    Scenario: Deleting a staff member should return no content
        Given a staff member exists
        When the staff member is deleted
        Then the staff delete response should indicate no content

    Scenario: Adding a staff member with an invalid role should return a bad request response
        Given a staff member request with an invalid role
        When the staff member is submitted
        Then the staff response should indicate bad request
