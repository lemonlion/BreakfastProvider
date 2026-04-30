Feature: Goat Milk Feature Flag
    /goat-milk - Goat milk availability controlled by feature flag

    @IgnoreIfExternalSut
    Scenario: Goat milk endpoint should return not found when feature is disabled
        Given the goat milk feature flag is disabled
        When goat milk is requested
        Then the goat milk response should indicate feature disabled

    @IgnoreIfExternalSut
    Scenario: Goat milk endpoint should return fresh goat milk when feature is enabled
        Given the goat milk feature flag is enabled
        When goat milk is requested
        Then the goat milk response should contain fresh goat milk
