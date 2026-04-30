using SrcHealthCheckNames = BreakfastProvider.Api.Services.HealthChecks.HealthCheckNames;

namespace BreakfastProvider.Tests.Component.Shared.Constants;

public static class HealthCheckStatuses
{
    public const string Healthy = "Healthy";
    public const string Degraded = "Degraded";
    public const string Unhealthy = "Unhealthy";
}

public static class HealthCheckNames
{
    public const string CowService = SrcHealthCheckNames.CowService;
    public const string GoatService = SrcHealthCheckNames.GoatService;
    public const string SupplierService = SrcHealthCheckNames.SupplierService;
    public const string KitchenService = SrcHealthCheckNames.KitchenService;
    public const string CosmosDb = SrcHealthCheckNames.CosmosDb;
    public const string Kafka = SrcHealthCheckNames.Kafka;
    public const string PubSub = SrcHealthCheckNames.PubSub;
    public const string Spanner = SrcHealthCheckNames.Spanner;
}
