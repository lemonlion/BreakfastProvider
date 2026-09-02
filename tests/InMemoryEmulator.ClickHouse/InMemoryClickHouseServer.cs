using System.Data.Common;
using ClickHouse.Client.ADO;
using InMemoryEmulator.ClickHouse.Core;
using InMemoryEmulator.ClickHouse.Http;

namespace InMemoryEmulator.ClickHouse;

/// <summary>
/// An in-process ClickHouse emulator for tests: a DuckDB-backed query engine behind an
/// <see cref="HttpMessageHandler"/> that speaks the ClickHouse HTTP interface as
/// <c>ClickHouse.Client</c> uses it. Create one per process, share it between test hosts and
/// isolate tests with randomised keys.
/// </summary>
public sealed class InMemoryClickHouseServer : IDisposable
{
    private readonly IClickHouseQueryEngine _engine;

    public InMemoryClickHouseServer(Action<InMemoryClickHouseOptions>? configure = null)
    {
        var options = new InMemoryClickHouseOptions();
        configure?.Invoke(options);

        Database = options.Database;
        _engine = new DuckDbClickHouseQueryEngine(Database);
        foreach (var script in options.DdlScripts)
            _engine.ExecuteDdl(script);

        Handler = new InMemoryClickHouseHandler(_engine);
    }

    public string Database { get; }

    /// <summary>The handler to put behind an <see cref="HttpClient"/> for the driver.</summary>
    public HttpMessageHandler Handler { get; }

    /// <summary>
    /// A connection string the driver accepts. The host is a placeholder (nothing listens);
    /// <c>Compression=false</c> keeps captured request bodies readable when debugging — the handler
    /// copes either way.
    /// </summary>
    public string ConnectionString => $"Host=inmemory;Port=8123;Compression=false;Database={Database}";

    public HttpClient CreateHttpClient() => new(Handler, disposeHandler: false);

    /// <summary>A <c>ClickHouse.Client</c> connection routed through the emulator.</summary>
    public DbConnection CreateConnection() => new ClickHouseConnection(ConnectionString, CreateHttpClient());

    /// <summary>Runs additional DDL after construction.</summary>
    public void ExecuteDdl(string sql) => _engine.ExecuteDdl(sql);

    public void Dispose()
    {
        Handler.Dispose();
        _engine.Dispose();
    }
}
