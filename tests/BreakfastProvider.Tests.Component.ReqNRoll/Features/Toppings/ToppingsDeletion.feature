Feature: Toppings Deletion
    /toppings - Deleting toppings from the system

    Rule: Existing toppings can be deleted

        @happy-path
        Scenario: Deleting an existing topping should return no content
            Given a known topping exists
            When the topping is deleted
            Then the delete response should indicate success

    Rule: Non-existent toppings cannot be deleted

        Scenario: Deleting a non-existent topping should return not found
            Given a topping id that does not exist
            When the topping is deleted
            Then the delete response should indicate not found
