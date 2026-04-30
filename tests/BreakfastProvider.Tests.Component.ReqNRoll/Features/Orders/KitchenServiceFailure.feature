Feature: Kitchen Service Failure
    /orders - Creating orders when the Kitchen Service returns an error

    @SkipUnlessFakesControllable
    Scenario: Creating an order when the kitchen service returns an error should still create the order
        Given a pancake batch has been created
        And a valid order request for the created batch
        And the kitchen service is configured to return busy
        When the breakfast order is placed
        Then the order should still be created successfully despite the kitchen failure
