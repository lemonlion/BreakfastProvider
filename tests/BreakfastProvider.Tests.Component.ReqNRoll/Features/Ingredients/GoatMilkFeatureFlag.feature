Feature: Goat Milk Feature Flag
    /goat-milk - Goat milk availability controlled by feature flag

    Rule: Goat milk is available when the feature flag is enabled

        @IgnoreIfExternalSut
        Scenario: Goat milk endpoint should return fresh goat milk when feature is enabled
            Given the goat milk feature flag is enabled
            When goat milk is requested
            Then the goat milk response should contain fresh goat milk

    Rule: Goat milk is hidden when the feature flag is disabled

        @IgnoreIfExternalSut
        Scenario: Goat milk endpoint should return not found when feature is disabled
            Given the goat milk feature flag is disabled
            When goat milk is requested
            Then the goat milk response should indicate feature disabled
