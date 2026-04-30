Feature: Toppings Deletion
    /toppings - Deleting toppings from the system

    @happy-path
    Scenario: Deleting an existing topping should return no content
        Given a known topping exists
        When the topping is deleted
        Then the delete response should indicate success

    Scenario: Deleting a non-existent topping should return not found
        Given a topping id that does not exist
        When the topping is deleted
        Then the delete response should indicate not found
