namespace BreakfastProvider.Tests.Component.Shared.Constants;

public static class EventTypes
{
    public const string OrderCreated = "OrderCreatedEvent";
}

public static class OutboxStatuses
{
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}
