using System.Data.Common;
using ClickHouse.Driver;
using ClickHouse.Driver.ADO;
using InMemoryEmulator.ClickHouse;
using static BreakfastProvider.Tests.Unit.Emulator.TestSupport;

namespace BreakfastProvider.Tests.Unit.Emulator;

/// <summary>
/// The same driver-level behaviour tests against the emulator and against a real ClickHouse.
/// The real backend runs only when <c>CLICKHOUSE_CONFORMANCE_CONNECTION_STRING</c> is set (CI sets it
/// in the Docker-lane jobs; locally <c>docker compose -f docker/docker-compose-database.yml up -d clickhouse</c>
/// then <c>Host=localhost;Port=8123;Database=kitchen_analytics</c>). Any drift — a type string, an error
/// code, a null — fails here, at the driver level, before it can show up as a diagram difference.
/// </summary>
public sealed class ClickHouseConformanceTests : IDisposable
{
    public const string RealConnectionStringVariable = "CLICKHOUSE_CONFORMANCE_CONNECTION_STRING";

    private static readonly Lazy<InMemoryClickHouseServer> Emulator = new(() => new InMemoryClickHouseServer(o =>
    {
        o.Database = "kitchen_analytics";
        o.ExecuteDdlFile(DdlPath);
    }));

    public static TheoryData<string> Backends => new() { "InMemory", "Real" };

    private readonly List<DbConnection> _connections = [];

    public void Dispose()
    {
        foreach (var connection in _connections)
            connection.Dispose();
    }

    [Theory]
    [MemberData(nameof(Backends))]
    public async Task Insert_summary_list_delete_round_trip(string backend)
    {
        var connection = await Open(backend);
        var station = $"Griddle-{Guid.NewGuid():N}";
        var recordedAt = new DateTime(2026, 9, 2, 10, 11, 12, DateTimeKind.Utc);
        var firstId = Guid.NewGuid().ToString();
        var secondId = Guid.NewGuid().ToString();

        await Insert(connection, firstId, station, 12.5d, recordedAt);
        await Insert(connection, secondId, station, 40d, recordedAt.AddMinutes(1));

        await using (var summary = connection.CreateCommand())
        {
            summary.CommandText =
                "SELECT station, avg(prep_seconds) AS avg_prep_seconds, quantile(0.95)(prep_seconds) AS p95_prep_seconds, count() AS timing_count " +
                "FROM order_timings WHERE station = {station:String} GROUP BY station ORDER BY avg_prep_seconds DESC";
            Add(summary, "station", station);

            await using var reader = await summary.ExecuteReaderAsync();
            reader.FieldCount.Should().Be(4);
            Enumerable.Range(0, 4).Select(reader.GetName).Should().Equal("station", "avg_prep_seconds", "p95_prep_seconds", "timing_count");
            Enumerable.Range(0, 4).Select(reader.GetDataTypeName).Should().Equal("String", "Float64", "Float64", "UInt64");
            Enumerable.Range(0, 4).Select(reader.GetFieldType).Should().Equal(typeof(string), typeof(double), typeof(double), typeof(ulong));

            (await reader.ReadAsync()).Should().BeTrue();
            reader.GetString(0).Should().Be(station);
            reader.GetDouble(1).Should().Be(26.25d);
            reader.GetDouble(2).Should().BeApproximately(38.625d, 0.001);
            reader.GetValue(3).Should().Be(2UL);
            Convert.ToInt32(reader["timing_count"]).Should().Be(2);
            (await reader.ReadAsync()).Should().BeFalse();
        }

        await using (var list = connection.CreateCommand())
        {
            list.CommandText =
                "SELECT timing_id, order_id, station, item_type, prep_seconds, recorded_at FROM order_timings " +
                "WHERE station = {station:String} ORDER BY recorded_at DESC";
            Add(list, "station", station);

            await using var reader = await list.ExecuteReaderAsync();
            Enumerable.Range(0, 6).Select(reader.GetDataTypeName).Should().Equal("String", "String", "String", "String", "Float64", "DateTime"); // ClickHouse.Driver reports the bare column type (ClickHouse.Client used to append the handshake server timezone)
            Enumerable.Range(0, 6).Select(reader.GetFieldType).Should().Equal(typeof(string), typeof(string), typeof(string), typeof(string), typeof(double), typeof(DateTime));

            (await reader.ReadAsync()).Should().BeTrue();
            reader.GetString(0).Should().Be(secondId, "ORDER BY recorded_at DESC puts the later row first");
            reader.GetDateTime(5).Should().Be(recordedAt.AddMinutes(1));
            reader.GetDateTime(5).Kind.Should().Be(DateTimeKind.Unspecified); // ClickHouse.Driver returns zoneless DateTimes as Unspecified (ClickHouse.Client stamped Utc); services SpecifyKind at their read sites
            (await reader.ReadAsync()).Should().BeTrue();
            reader.GetString(0).Should().Be(firstId);
            reader.GetDouble(4).Should().Be(12.5d);
            (await reader.ReadAsync()).Should().BeFalse();
        }

        await using (var delete = connection.CreateCommand())
        {
            delete.CommandText = "DELETE FROM order_timings WHERE timing_id = {id:String}";
            Add(delete, "id", firstId);
            await delete.ExecuteNonQueryAsync();
        }

        await using (var count = connection.CreateCommand())
        {
            count.CommandText = "SELECT count() FROM order_timings WHERE station = {station:String}";
            Add(count, "station", station);
            (await count.ExecuteScalarAsync()).Should().Be(1UL, "the lightweight DELETE is visible to the next SELECT");
        }
    }

    [Theory]
    [MemberData(nameof(Backends))]
    public async Task ExecuteNonQuery_returns_zero_for_insert_and_delete(string backend)
    {
        var connection = await Open(backend);
        var id = Guid.NewGuid().ToString();

        (await Insert(connection, id, $"Station-{Guid.NewGuid():N}", 1d, DateTime.UtcNow)).Should().Be(0);

        await using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM order_timings WHERE timing_id = {id:String}";
        Add(delete, "id", id);
        (await delete.ExecuteNonQueryAsync()).Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(Backends))]
    public async Task ExecuteScalar_select_1_is_a_byte(string backend)
    {
        var connection = await Open(backend);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";

        (await command.ExecuteScalarAsync()).Should().Be((byte)1);
    }

    [Theory]
    [MemberData(nameof(Backends))]
    public async Task Error_codes_match_the_server(string backend)
    {
        var connection = await Open(backend);

        (await Fail(connection, "SELECT * FROM nope")).Should().StartWith("Code: 60.");
        (await Fail(connection, "SELEC 1")).Should().StartWith("Code: 62.");
        (await Fail(connection, "SELECT BAD SYNTAX")).Should().StartWith("Code: 47.");
    }

    [Theory]
    [MemberData(nameof(Backends))]
    public async Task DateTime_parameters_round_trip_to_the_second_as_utc(string backend)
    {
        var connection = await Open(backend);
        var id = Guid.NewGuid().ToString();
        var station = $"Clock-{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        var expected = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

        await Insert(connection, id, station, 1d, now);

        await using var select = connection.CreateCommand();
        select.CommandText = "SELECT recorded_at FROM order_timings WHERE timing_id = {id:String}";
        Add(select, "id", id);
        var stored = (DateTime)(await select.ExecuteScalarAsync())!;

        stored.Should().Be(expected);
        stored.Kind.Should().Be(DateTimeKind.Unspecified); // ClickHouse.Driver returns zoneless DateTimes as Unspecified (ClickHouse.Client stamped Utc)
    }

    private async Task<DbConnection> Open(string backend)
    {
        DbConnection connection;
        if (backend == "InMemory")
        {
            connection = Emulator.Value.CreateConnection();
        }
        else
        {
            var connectionString = Environment.GetEnvironmentVariable(RealConnectionStringVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
                Assert.Skip($"{RealConnectionStringVariable} is not set — start the ClickHouse container and set it to run the conformance suite against a real server.");

            connection = new ClickHouseConnection(connectionString);
        }

        _connections.Add(connection);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<int> Insert(DbConnection connection, string timingId, string station, double prepSeconds, DateTime recordedAt)
    {
        await using var insert = connection.CreateCommand();
        insert.CommandText =
            "INSERT INTO order_timings (timing_id, order_id, station, item_type, prep_seconds, recorded_at) " +
            "VALUES ({timingId:String}, {orderId:String}, {station:String}, {itemType:String}, {prepSeconds:Float64}, {recordedAt:DateTime})";
        Add(insert, "timingId", timingId);
        Add(insert, "orderId", Guid.NewGuid().ToString());
        Add(insert, "station", station);
        Add(insert, "itemType", "Pancakes");
        Add(insert, "prepSeconds", prepSeconds);
        Add(insert, "recordedAt", recordedAt);
        return await insert.ExecuteNonQueryAsync();
    }

    private static async Task<string> Fail(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var act = () => command.ExecuteReaderAsync();
        return (await act.Should().ThrowAsync<ClickHouseServerException>()).Which.Message;
    }

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
