using BreakfastProvider.Api;
using BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration;
using Kronikol;
using Kronikol.Constants;
using Kronikol.Parties;

namespace BreakfastProvider.Tests.Component.Shared;

/// <summary>
/// Who each party in a run is (Kronikol 5.14.0+), so the report, the history ledger and <c>kronikol query parties</c>
/// can say which service the tests are about and, for each dependency, whether its own software answered or
/// something stood in for it. Each declared stand-in is drawn with «test double» on its participant.
/// </summary>
public static class KronikolParties
{
    /// <summary>The id this service goes by in every report, the run line of the history ledger and CTRF's appName.</summary>
    public const string ServiceId = "breakfast-provider";

    /// <summary>
    /// Declares the parties for the lane these settings describe. Each <c>RunWithAnInMemory*</c> flag swaps one
    /// dependency for an in-process fake; with the flag off the dependency is the container that
    /// <c>docker/docker-compose-*.yml</c> starts. A container running the product's own image (SQL Server, MongoDB,
    /// ClickHouse, Kafka) is real. A vendor's emulator or simulator (Cosmos DB, Spanner, Pub/Sub, BigQuery, Event Hubs,
    /// Event Grid) and the sibling-service fakes under <c>fakes/</c> stand in for the real thing wherever they run, so
    /// only the note says which one answered.
    /// </summary>
    public static ReportConfigurationOptions WithParties(this ReportConfigurationOptions options, ComponentTestSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        options.ServiceId = ServiceId;
        options.Parties.Add(new(Documentation.ServiceNames.BreakfastProvider) { Kind = PartyKind.UnderTest });

        // The test's own client keeps Kronikol's default caller name in every framework's CreateTestClient.
        options.Parties.Add(new(TrackingDefaults.CallerName) { Kind = PartyKind.Test });

        // Against an external SUT the service runs in its own container, and the test process sees only its own
        // calls to it. A dependency declared here would be a name no call in the report carries, which Kronikol
        // reports as a diagnostic.
        if (settings.RunAgainstExternalServiceUnderTest)
            return options;

        RealInDocker(Documentation.ServiceNames.BreakfastDatabase, settings.RunWithAnInMemoryBreakfastDatabase, "SQLite in memory", "SQL Server in docker");
        RealInDocker(Documentation.ServiceNames.ReportingDatabase, settings.RunWithAnInMemoryReportingDatabase, "SQLite in memory", "SQL Server in docker");
        RealInDocker(Documentation.ServiceNames.MongoDB, settings.RunWithAnInMemoryMongoDatabase, "InMemoryEmulator.MongoDB in process", "MongoDB in docker");
        RealInDocker(Documentation.ServiceNames.ClickHouse, settings.RunWithAnInMemoryClickHouse, "InMemoryEmulator.ClickHouse in process", "ClickHouse in docker");
        RealInDocker(Documentation.ServiceNames.KafkaBroker, settings.RunWithAnInMemoryKafkaBroker, "in-memory producers and consumers", "Kafka in docker");

        AlwaysDouble(Documentation.ServiceNames.CosmosDb, settings.RunWithAnInMemoryDatabase, "CosmosDB.InMemoryEmulator in process", "Cosmos DB emulator in docker");
        AlwaysDouble(Documentation.ServiceNames.Spanner, settings.RunWithAnInMemorySpannerDatabase, "Spanner.InMemoryEmulator in process", "Spanner emulator in docker");
        AlwaysDouble(Documentation.ServiceNames.BigQuery, settings.RunWithAnInMemoryBigQuery, "InMemoryEmulator.BigQuery in process", "goccy/bigquery-emulator in docker");
        AlwaysDouble(Documentation.ServiceNames.GoogleCloudPubSub, settings.RunWithAnInMemoryPubSub, "in-memory publishers", "Pub/Sub emulator in docker");
        AlwaysDouble(Documentation.ServiceNames.AzureEventHub, settings.RunWithAnInMemoryEventHub, "in-memory publishers", "Event Hubs emulator in docker");
        AlwaysDouble(Documentation.ServiceNames.EventGrid, settings.RunWithAnInMemoryEventGrid, "in-memory publishers", "Event Grid Simulator in docker");

        // The names TestHttpClientFactory gives the sibling services' calls.
        AlwaysDouble("Cow Service", settings.RunWithAnInMemoryCowService, "Dependencies.Fakes.CowService in process", "Dependencies.Fakes.CowService in docker");
        AlwaysDouble("Goat Service", settings.RunWithAnInMemoryGoatService, "Dependencies.Fakes.GoatService in process", "Dependencies.Fakes.GoatService in docker");
        AlwaysDouble("Supplier Service", settings.RunWithAnInMemorySupplierService, "Dependencies.Fakes.SupplierService in process", "Dependencies.Fakes.SupplierService in docker");
        AlwaysDouble("Kitchen Service", settings.RunWithAnInMemoryKitchenService, "Dependencies.Fakes.KitchenService in process", "Dependencies.Fakes.KitchenService in docker");
        AlwaysDouble(Documentation.ServiceNames.NotificationService, settings.RunWithAnInMemoryNotificationService, "Dependencies.Fakes.NotificationService in process", "Dependencies.Fakes.NotificationService in docker");

        return options;

        void RealInDocker(string name, bool inMemory, string fake, string container) =>
            options.Parties.Add(new(name) { Kind = inMemory ? PartyKind.Double : PartyKind.Real, Note = inMemory ? fake : container });

        void AlwaysDouble(string name, bool inMemory, string fake, string standIn) =>
            options.Parties.Add(new(name) { Kind = PartyKind.Double, Note = inMemory ? fake : standIn });
    }
}
