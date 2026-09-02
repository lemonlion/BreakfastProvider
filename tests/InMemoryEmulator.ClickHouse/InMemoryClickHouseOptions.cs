namespace InMemoryEmulator.ClickHouse;

public sealed class InMemoryClickHouseOptions
{
    /// <summary>
    /// The one database the emulator serves. Statements may qualify tables with it
    /// (<c>kitchen_analytics.order_timings</c>); the qualifier is stripped.
    /// </summary>
    public string Database { get; set; } = "default";

    internal List<string> DdlScripts { get; } = [];

    /// <summary>Queues a ClickHouse DDL script (one or more <c>;</c>-separated statements) to run when the server is created.</summary>
    public InMemoryClickHouseOptions ExecuteDdl(string sql)
    {
        DdlScripts.Add(sql);
        return this;
    }

    /// <summary>Queues the DDL in a <c>.sql</c> file — typically the same file that seeds the Docker container.</summary>
    public InMemoryClickHouseOptions ExecuteDdlFile(string path) => ExecuteDdl(File.ReadAllText(path));
}
