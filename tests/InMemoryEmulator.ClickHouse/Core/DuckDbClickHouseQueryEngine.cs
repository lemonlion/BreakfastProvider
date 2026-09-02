using System.Data;
using DuckDB.NET.Data;

namespace InMemoryEmulator.ClickHouse.Core;

/// <summary>
/// <see cref="IClickHouseQueryEngine"/> backed by a single in-memory DuckDB connection. Every call
/// is serialised through a semaphore: tests run in parallel and serialising sub-millisecond queries
/// is the simplest correct answer.
/// </summary>
public sealed class DuckDbClickHouseQueryEngine : IClickHouseQueryEngine
{
    private readonly DuckDBConnection _connection = new("DataSource=:memory:");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ClickHouseSqlTranslator _translator;

    public DuckDbClickHouseQueryEngine(string database)
    {
        _translator = new ClickHouseSqlTranslator(database);
        _connection.Open();
    }

    public void ExecuteDdl(string sql)
    {
        _gate.Wait();
        try
        {
            foreach (var statement in SplitStatements(sql))
            {
                var translated = Guard(() => _translator.TranslateDdl(statement));
                if (translated is null) continue;

                using var command = _connection.CreateCommand();
                command.CommandText = translated;
                Guard(() => command.ExecuteNonQuery());
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public ClickHouseResultSet Execute(string sql, IReadOnlyDictionary<string, string> parameters)
    {
        _gate.Wait();
        try
        {
            using var command = PrepareCommand(sql, parameters);
            using var reader = Guard(() => command.ExecuteReader(CommandBehavior.Default));
            return Materialise(reader);
        }
        finally
        {
            _gate.Release();
        }
    }

    public int ExecuteNonQuery(string sql, IReadOnlyDictionary<string, string> parameters)
    {
        _gate.Wait();
        try
        {
            if (ClickHouseSqlTranslator.IsDdl(sql))
            {
                var translated = Guard(() => _translator.TranslateDdl(sql));
                if (translated is null) return 0;

                using var ddl = _connection.CreateCommand();
                ddl.CommandText = translated;
                Guard(() => ddl.ExecuteNonQuery());
                return 0;
            }

            using var command = PrepareCommand(sql, parameters);
            return Guard(() => command.ExecuteNonQuery());
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        _gate.Dispose();
    }

    private DuckDBCommand PrepareCommand(string sql, IReadOnlyDictionary<string, string> parameters)
    {
        var translated = Guard(() => _translator.TranslateQuery(sql));
        var (duckSql, bound) = Guard(() => ClickHouseParameterBinder.Bind(translated, parameters));

        var command = _connection.CreateCommand();
        command.CommandText = duckSql;
        foreach (var parameter in bound)
            command.Parameters.Add(new DuckDBParameter(parameter.Name, parameter.Value));

        return command;
    }

    private static ClickHouseResultSet Materialise(DuckDBDataReader reader)
    {
        var fieldCount = reader.FieldCount;
        var rows = new List<object?[]>();
        var hasNull = new bool[fieldCount];

        while (reader.Read())
        {
            var row = new object?[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                if (value is DBNull)
                    hasNull[i] = true;
                else if (value is DateTime dateTime)
                    // DuckDB TIMESTAMP values come back Kind=Unspecified; the emulator's clock is UTC.
                    value = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

                row[i] = value;
            }

            rows.Add(row);
        }

        var columns = new ClickHouseColumn[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            var typeString = Guard(() => ClickHouseTypeMap.ClrTypeToClickHouse(reader.GetFieldType(i)));
            if (hasNull[i])
                typeString = $"Nullable({typeString})";

            columns[i] = new ClickHouseColumn(reader.GetName(i), typeString);
        }

        return new ClickHouseResultSet(columns, rows);
    }

    private static IEnumerable<string> SplitStatements(string script) =>
        script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(statement => statement.Length > 0);

    /// <summary>Maps DuckDB and translator failures to <see cref="ClickHouseEmulatorException"/>s with ClickHouse error codes.</summary>
    private static T Guard<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (ClickHouseEmulatorException)
        {
            throw;
        }
        catch (NotSupportedException ex)
        {
            throw ClickHouseEmulatorException.NotImplemented(ex.Message, ex);
        }
        catch (DuckDBException ex)
        {
            throw MapDuckDbException(ex);
        }
        catch (FormatException ex)
        {
            throw ClickHouseEmulatorException.SyntaxError(ex.Message, ex);
        }
    }

    private static ClickHouseEmulatorException MapDuckDbException(DuckDBException ex)
    {
        var message = ex.Message.Trim();

        if (message.StartsWith("Parser Error", StringComparison.OrdinalIgnoreCase))
            return ClickHouseEmulatorException.SyntaxError(StripPrefix(message), ex);
        if (message.StartsWith("Catalog Error", StringComparison.OrdinalIgnoreCase))
            return ClickHouseEmulatorException.UnknownTable(StripPrefix(message), ex);
        if (message.StartsWith("Binder Error", StringComparison.OrdinalIgnoreCase))
            return ClickHouseEmulatorException.UnknownIdentifier(StripPrefix(message), ex);

        return ClickHouseEmulatorException.Generic(message, ex);

        static string StripPrefix(string text)
        {
            var colon = text.IndexOf(':');
            return colon < 0 ? text : text[(colon + 1)..].Trim();
        }
    }
}
