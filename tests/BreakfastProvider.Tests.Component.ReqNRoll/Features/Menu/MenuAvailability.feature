Feature: Menu Availability
    /menu - Checking menu item availability from ingredient sources

    @happy-path
    Scenario: The menu endpoint should return all menu items with availability status
        When the menu is requested
        Then the menu response should contain all menu items
