Feature: Chef Notes Management
    /chef-notes - Creating, retrieving, and updating chef notes (MongoDB)

    @happy-path
    Scenario: Creating a chef note should return the created note
        Given a valid chef note request
        When the note is submitted
        Then the response should contain the created note

    Scenario: Retrieving an existing note by id should return the note
        Given a chef note exists
        When the note is retrieved by id
        Then the get response should contain the note

    Scenario: Updating an existing note should return the updated note
        Given a chef note exists
        When the note is updated
        Then the update response should contain the modified note

    Scenario: Listing notes by recipe should return matching notes
        Given a chef note exists
        When the notes are listed by recipe
        Then the list response should contain the note

    Scenario: Retrieving a non-existent note should return not found
        When a non-existent note is retrieved
        Then the get response should indicate not found

    Scenario: Updating a non-existent note should return not found
        When a non-existent note is updated
        Then the update response should indicate not found

    Scenario: Creating a note with missing recipe name should return bad request
        Given a note request with a missing recipe name
        When the note is submitted
        Then the note response should indicate bad request

    Scenario: Creating a note with missing note text should return bad request
        Given a note request with missing note text
        When the note is submitted
        Then the note response should indicate bad request
