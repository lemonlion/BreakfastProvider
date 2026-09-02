namespace InMemoryEmulator.ClickHouse.Core;

/// <summary>
/// Executes ClickHouse-dialect SQL. Parameters arrive exactly as the HTTP interface receives them:
/// the raw text of each <c>param_&lt;name&gt;</c> value, keyed by name; the engine coerces them
/// according to the <c>{name:Type}</c> placeholders in the SQL.
/// </summary>
public interface IClickHouseQueryEngine : IDisposable
{
    /// <summary>Runs a statement that returns rows (SELECT).</summary>
    ClickHouseResultSet Execute(string sql, IReadOnlyDictionary<string, string> parameters);

    /// <summary>Runs a statement that returns no rows (INSERT, DELETE) and returns the rows affected.</summary>
    int ExecuteNonQuery(string sql, IReadOnlyDictionary<string, string> parameters);

    /// <summary>Runs one or more <c>;</c>-separated DDL statements (CREATE DATABASE / CREATE TABLE / DROP TABLE).</summary>
    void ExecuteDdl(string sql);
}
