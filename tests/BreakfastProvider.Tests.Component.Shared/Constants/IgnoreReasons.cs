namespace BreakfastProvider.Tests.Component.Shared.Constants;

public static class IgnoreReasons
{
    // ── Post-deployment skip reasons (step-level) ────────────────────────────
    public const string EventStoreIsUnavailableInPostDeploymentEnvironments = "Event store is unavailable in post-deployment environments";
    public const string DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments = "Downstream fake request store is unavailable in post-deployment environments";
    public const string KafkaIsUnavailableInPostDeploymentEnvironments = "Kafka message store is unavailable in post-deployment environments";
    public const string OutboxStoreIsUnavailableInPostDeploymentEnvironments = "Outbox store is unavailable in post-deployment environments";

    // ── Post-deployment skip reasons (scenario-level) ────────────────────────
    public const string NeedsToControlFakeResponses = "This test needs to control downstream fake responses which is not possible in post-deployment environments";
    public const string NeedsNonDefaultConfiguration = "This test requires configuration overrides which are not possible in post-deployment environments where only the default config is available";
    public const string NeedsDirectDatabaseAccess = "This test needs direct database access which is not available in post-deployment environments";
    public const string NeedsEventAndKafkaInfrastructure = "This test's primary assertions depend on event store and/or Kafka which are unavailable in post-deployment environments";

    // ── Docker-mode skip reasons (scenario-level) ────────────────────────────
    public const string NeedsIsolatedDatabase = "This test assumes an empty database which is not guaranteed when using a shared Docker database";
}
