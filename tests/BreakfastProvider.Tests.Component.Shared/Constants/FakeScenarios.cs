namespace BreakfastProvider.Tests.Component.Shared.Constants;

public static class FakeScenarios
{
    public const string ServiceUnavailable = "ServiceUnavailable";
    public const string Timeout = "Timeout";
    public const string OutOfStock = "OutOfStock";
    public const string KitchenBusy = "KitchenBusy";
    public const string InvalidResponse = "InvalidResponse";
}

public static class FakeScenarioHeaders
{
    public const string CowService = "X-Fake-CowService-Scenario";
    public const string GoatService = "X-Fake-GoatService-Scenario";
    public const string SupplierService = "X-Fake-SupplierService-Scenario";
    public const string KitchenService = "X-Fake-KitchenService-Scenario";
}

public static class DownstreamErrorMessages
{
    public const string CowServiceUnavailable = "The Cow Service returned an error.";
    public const string CowServiceUnreachable = "The Cow Service is unreachable.";
    public const string GoatServiceUnavailable = "The Goat Service returned an error.";
    public const string GoatServiceUnreachable = "The Goat Service is unreachable.";
    public const string CowServiceUnavailableTitle = "Cow Service Unavailable";
    public const string GoatServiceUnavailableTitle = "Goat Service Unavailable";
    public const string FeatureDisabled = "Feature Disabled";
}
