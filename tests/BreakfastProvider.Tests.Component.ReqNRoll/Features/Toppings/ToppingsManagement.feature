Feature: Toppings Management
    /toppings - Listing available toppings and adding custom toppings

    @happy-path
    Scenario: The toppings endpoint should return all available toppings
        When the available toppings are requested
        Then the toppings response should contain the default toppings

    Scenario: Adding a new topping should return the created topping
        Given a valid topping request
        When the new topping is submitted
        Then the topping response should contain the created topping
