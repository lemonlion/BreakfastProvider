using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BreakfastProvider.Api.Services.HealthChecks;

public static class HealthCheckNames
{
    public const string CowService = "CowService";
    public const string GoatService = "GoatService";
    public const string SupplierService = "SupplierService";
    public const string KitchenService = "KitchenService";
    public const string CosmosDb = "CosmosDb";
    public const string Kafka = "Kafka";
    public const string PubSub = "PubSub";
    public const string Spanner = "Spanner";
}

public static class HealthCheckTags
{
    public const string Downstream = "downstream";
    public const string Api = "api";
    public const string Infrastructure = "infrastructure";
    public const string Database = "database";
    public const string Messaging = "messaging";
}

public static class HealthCheckServiceExtensions
{
    private const string HealthEndpoint = "health";

    public static IHealthChecksBuilder AddDownstreamServiceChecks(this IHealthChecksBuilder builder)
    {
        string[] serviceNames =
        [
            HealthCheckNames.CowService,
            HealthCheckNames.GoatService,
            HealthCheckNames.SupplierService,
            HealthCheckNames.KitchenService
        ];

        foreach (var name in serviceNames)
        {
            builder.Add(new HealthCheckRegistration(
                name,
                sp => new DownstreamServiceHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), name, HealthEndpoint),
                failureStatus: HealthStatus.Degraded,
                tags: [HealthCheckTags.Downstream, HealthCheckTags.Api]));
        }

        return builder;
    }

    public static IHealthChecksBuilder AddInfrastructureChecks(this IHealthChecksBuilder builder)
    {
        builder.Add(new HealthCheckRegistration(
            HealthCheckNames.CosmosDb,
            sp => new CosmosDbHealthCheck(sp.GetService<CosmosClient>()),
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Infrastructure, HealthCheckTags.Database]));

        builder.Add(new HealthCheckRegistration(
            HealthCheckNames.Kafka,
            sp =>
            {
                var kafkaCheck = sp.GetService<KafkaHealthCheck>();
                return kafkaCheck ?? (IHealthCheck)new NoOpHealthCheck("Kafka not configured.");
            },
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Infrastructure, HealthCheckTags.Messaging]));

        builder.Add(new HealthCheckRegistration(
            HealthCheckNames.PubSub,
            sp =>
            {
                var pubSubCheck = sp.GetService<PubSubHealthCheck>();
                return pubSubCheck ?? (IHealthCheck)new NoOpHealthCheck("Pub/Sub not configured.");
            },
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Infrastructure, HealthCheckTags.Messaging]));

        builder.Add(new HealthCheckRegistration(
            HealthCheckNames.Spanner,
            sp =>
            {
                var spannerCheck = sp.GetService<SpannerHealthCheck>();
                return spannerCheck ?? (IHealthCheck)new NoOpHealthCheck("Spanner not configured.");
            },
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Infrastructure, HealthCheckTags.Database]));

        return builder;
    }
}
