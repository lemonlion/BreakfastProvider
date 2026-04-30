Feature: Menu Downstream Failure
    /menu - Menu behaviour when Supplier Service is unavailable

    @SkipUnlessFakesControllable
    Scenario: All menu items should be marked unavailable when the supplier service is down
        Given the supplier service will return service unavailable
        When the menu is requested
        Then the menu response should mark all items as unavailable
