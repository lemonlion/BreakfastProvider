Feature: Daily Specials Ordering
    /daily-specials - Ordering daily specials with threshold limits

    @happy-path
    Scenario: A valid daily special order should return a confirmation
        Given the cinnamon swirl order count is reset
        And a valid daily special order request for cinnamon swirl
        When the daily special order is submitted
        Then the daily special order response should contain a valid confirmation

    Scenario: The daily specials endpoint should return all available specials
        When the available daily specials are requested
        Then the daily specials response should contain all expected specials

    @IgnoreIfExternalSut
    Scenario: Ordering a daily special beyond the threshold should return a conflict response
        Given the matcha waffles order count is reset
        And the matcha waffles special has been ordered up to the configured limit
        When another order is placed for the matcha waffles special
        Then the response should indicate the daily special is sold out

    @IgnoreIfExternalSut
    Scenario: Remaining quantity should decrease after each order
        Given the lemon ricotta order count is reset
        And a daily special order for lemon ricotta of quantity one is placed
        When the available daily specials are requested
        Then the lemon ricotta special should have one fewer remaining
