using InMemoryEmulator.ClickHouse;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.ClickHouse;

/// <summary>
/// One process-wide in-memory ClickHouse, like the Cosmos and Mongo emulators: every
/// <c>WebApplicationFactory</c> shares it and tests isolate themselves with randomised keys.
/// Seeded from the same DDL file that seeds the Docker container, so the schema cannot drift
/// between lanes.
/// </summary>
public static class SharedInMemoryClickHouse
{
    public const string DatabaseName = "kitchen_analytics";

    private static readonly Lazy<InMemoryClickHouseServer> Lazy = new(() => new InMemoryClickHouseServer(options =>
    {
        options.Database = DatabaseName;
        options.ExecuteDdlFile(Path.Combine(AppContext.BaseDirectory, "Fakes", "ClickHouse", "001-kitchen-analytics.sql"));
    }));

    public static InMemoryClickHouseServer Server => Lazy.Value;
}
