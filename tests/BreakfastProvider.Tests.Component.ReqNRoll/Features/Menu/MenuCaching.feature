Feature: Menu Caching
    /menu - Menu response caching behaviour

    @SkipUnlessFakesControllable
    Scenario: Menu responses should be cached and returned even when supplier becomes unavailable
        Given the menu has been requested and cached
        And the supplier service is then made unavailable
        When the menu is requested again
        Then the menu response should still return available items
