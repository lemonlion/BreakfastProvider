Feature: Toppings Feature Flag
    /toppings - Raspberry topping availability controlled by feature flag

    @IgnoreIfExternalSut
    Scenario: Toppings should include raspberries when the feature flag is enabled
        Given the raspberry topping feature flag is enabled
        When the available toppings are requested
        Then the toppings response should include raspberries

    @IgnoreIfExternalSut
    Scenario: Toppings should exclude raspberries when the feature flag is disabled
        Given the raspberry topping feature flag is disabled
        When the available toppings are requested
        Then the toppings response should not include raspberries
