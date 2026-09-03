using ClickHouse.Driver;
using InMemoryEmulator.ClickHouse;
using static BreakfastProvider.Tests.Unit.Emulator.TestSupport;

namespace BreakfastProvider.Tests.Unit.Emulator;

/// <summary>The real driver, in-process, no Docker: open, insert, read back, delete, and surface a server exception.</summary>
public sealed class InMemoryClickHouseServerEndToEndTests : IDisposable
{
    private readonly InMemoryClickHouseServer _server = new(o =>
    {
        o.Database = "kitchen_analytics";
        o.ExecuteDdlFile(DdlPath);
    });

    public void Dispose() => _server.Dispose();

    [Fact]
    public async Task Driver_opens_inserts_reads_back_and_deletes()
    {
        await using var connection = _server.CreateConnection();
        await connection.OpenAsync();

        // ClickHouse.Driver's ServerVersion property throws by design ("use SELECT version()").
        await using (var version = connection.CreateCommand())
        {
            version.CommandText = "SELECT version()";
            (await version.ExecuteScalarAsync()).Should().Be("25.8.33.6");
        }
        connection.Database.Should().Be("kitchen_analytics");

        var station = $"Griddle-{Guid.NewGuid():N}";
        var recordedAt = new DateTime(2026, 9, 2, 10, 11, 12, DateTimeKind.Utc);

        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText =
                "INSERT INTO order_timings (timing_id, order_id, station, item_type, prep_seconds, recorded_at) " +
                "VALUES ({timingId:String}, {orderId:String}, {station:String}, {itemType:String}, {prepSeconds:Float64}, {recordedAt:DateTime})";
            Add(insert, "timingId", "t-1");
            Add(insert, "orderId", Guid.NewGuid());
            Add(insert, "station", station);
            Add(insert, "itemType", "Pancakes");
            Add(insert, "prepSeconds", 12.5d);
            Add(insert, "recordedAt", recordedAt);

            (await insert.ExecuteNonQueryAsync()).Should().Be(0, "the driver reports 0 rows affected, as it does against a real server");
        }

        await using (var select = connection.CreateCommand())
        {
            select.CommandText = "SELECT timing_id, prep_seconds, recorded_at FROM order_timings WHERE station = {station:String}";
            Add(select, "station", station);

            await using var reader = await select.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue();
            reader.GetString(0).Should().Be("t-1");
            reader.GetDouble(1).Should().Be(12.5d);
            reader.GetDateTime(2).Should().Be(recordedAt);
            reader.GetDateTime(2).Kind.Should().Be(DateTimeKind.Unspecified); // ClickHouse.Driver returns zoneless DateTimes as Unspecified
            (await reader.ReadAsync()).Should().BeFalse();
        }

        await using (var delete = connection.CreateCommand())
        {
            delete.CommandText = "DELETE FROM order_timings WHERE timing_id = {id:String}";
            Add(delete, "id", "t-1");
            (await delete.ExecuteNonQueryAsync()).Should().Be(0);
        }

        await using (var scalar = connection.CreateCommand())
        {
            scalar.CommandText = "SELECT count() FROM order_timings WHERE station = {station:String}";
            Add(scalar, "station", station);
            (await scalar.ExecuteScalarAsync()).Should().Be(0UL);
        }
    }

    [Fact]
    public async Task Select_1_scalar_is_a_byte_like_the_server()
    {
        await using var connection = _server.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";

        (await command.ExecuteScalarAsync()).Should().Be((byte)1);
    }

    [Fact]
    public async Task Unknown_table_surfaces_as_a_clickhouse_server_exception()
    {
        await using var connection = _server.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM nope";

        var act = () => command.ExecuteReaderAsync();

        (await act.Should().ThrowAsync<ClickHouseServerException>()).Which.Message.Should().StartWith("Code: 60.");
    }

    [Fact]
    public async Task Compression_enabled_connections_work_too()
    {
        var handler = _server.Handler;
        await using var connection = new ClickHouse.Driver.ADO.ClickHouseConnection(
            "Host=inmemory;Port=8123;Database=kitchen_analytics", new HttpClient(handler, disposeHandler: false));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";

        (await command.ExecuteScalarAsync()).Should().Be((byte)1);
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
