Feature: Customer Preferences Management
    /customer-preferences - Managing customer breakfast preferences

    @happy-path
    Scenario: Saving customer preferences should return the saved preferences
        Given a valid customer preference request
        When the customer preferences are saved
        Then the preference response should contain the saved preferences

    Scenario: Retrieving existing customer preferences should return the preferences
        Given customer preferences exist
        When the customer preferences are retrieved
        Then the preference get response should contain the preferences

    Scenario: Updating customer preferences should return the updated preferences
        Given customer preferences exist
        When the customer preferences are updated
        Then the preference update response should contain the updated values

    Scenario: Retrieving non-existent customer preferences should return not found
        When non-existent customer preferences are retrieved
        Then the preference get response should indicate not found

    Scenario: Saving customer preferences with missing customer name should return bad request
        Given a customer preference request with missing customer name
        When the customer preferences are saved
        Then the preference response should indicate bad request
