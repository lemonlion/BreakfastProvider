using InMemoryEmulator.ClickHouse.Core;
using static BreakfastProvider.Tests.Unit.Emulator.TestSupport;

namespace BreakfastProvider.Tests.Unit.Emulator;

public sealed class DuckDbClickHouseQueryEngineTests : IDisposable
{
    private readonly DuckDbClickHouseQueryEngine _engine = new("kitchen_analytics");

    public DuckDbClickHouseQueryEngineTests() => _engine.ExecuteDdl(Ddl);

    public void Dispose() => _engine.Dispose();

    [Fact]
    public void Ddl_from_the_shared_file_creates_all_three_tables()
    {
        var tables = _engine.Execute("SHOW TABLES", NoParams);

        tables.Rows.Select(r => (string)r[0]!).Should().BeEquivalentTo("order_timings", "equipment_readings", "service_times");
    }

    [Fact]
    public void Insert_select_and_delete_round_trip_with_clickhouse_types_and_utc_datetimes()
    {
        var station = $"Griddle-{Guid.NewGuid():N}";

        var inserted = _engine.ExecuteNonQuery(
            "INSERT INTO order_timings (timing_id, order_id, station, item_type, prep_seconds, recorded_at) " +
            "VALUES ({timingId:String}, {orderId:String}, {station:String}, {itemType:String}, {prepSeconds:Float64}, {recordedAt:DateTime})",
            Params(("timingId", "t-1"), ("orderId", "o-1"), ("station", station), ("itemType", "Pancakes"), ("prepSeconds", "12.5"), ("recordedAt", "2026-09-02T10:11:12")));
        inserted.Should().Be(1);

        var list = _engine.Execute(
            "SELECT timing_id, order_id, station, item_type, prep_seconds, recorded_at FROM order_timings WHERE station = {station:String} ORDER BY recorded_at DESC",
            Params(("station", station)));

        list.Columns.Select(c => c.TypeString).Should().Equal("String", "String", "String", "String", "Float64", "DateTime");
        var row = list.Rows.Should().ContainSingle().Subject;
        row[0].Should().Be("t-1");
        row[4].Should().Be(12.5d);
        var recordedAt = row[5].Should().BeOfType<DateTime>().Subject;
        recordedAt.Should().Be(new DateTime(2026, 9, 2, 10, 11, 12));
        recordedAt.Kind.Should().Be(DateTimeKind.Utc);

        var deleted = _engine.ExecuteNonQuery("DELETE FROM order_timings WHERE timing_id = {id:String}", Params(("id", "t-1")));
        deleted.Should().Be(1);

        _engine.Execute("SELECT timing_id FROM order_timings WHERE station = {station:String}", Params(("station", station)))
            .Rows.Should().BeEmpty();
    }

    [Fact]
    public void Summary_query_returns_the_servers_column_types()
    {
        var station = $"Oven-{Guid.NewGuid():N}";
        foreach (var seconds in new[] { "10", "20", "30", "40" })
        {
            _engine.ExecuteNonQuery(
                "INSERT INTO order_timings (timing_id, order_id, station, item_type, prep_seconds, recorded_at) " +
                "VALUES ({timingId:String}, {orderId:String}, {station:String}, {itemType:String}, {prepSeconds:Float64}, {recordedAt:DateTime})",
                Params(("timingId", Guid.NewGuid().ToString()), ("orderId", "o"), ("station", station), ("itemType", "Waffles"), ("prepSeconds", seconds), ("recordedAt", "2026-09-02T10:11:12")));
        }

        var summary = _engine.Execute(
            "SELECT station, avg(prep_seconds) AS avg_prep_seconds, quantile(0.95)(prep_seconds) AS p95_prep_seconds, count() AS timing_count " +
            "FROM kitchen_analytics.order_timings WHERE station = {station:String} GROUP BY station ORDER BY avg_prep_seconds DESC",
            Params(("station", station)));

        summary.Columns.Select(c => c.Name).Should().Equal("station", "avg_prep_seconds", "p95_prep_seconds", "timing_count");
        summary.Columns.Select(c => c.TypeString).Should().Equal("String", "Float64", "Float64", "UInt64");
        var row = summary.Rows.Should().ContainSingle().Subject;
        row[1].Should().Be(25d);
        row[2].Should().Be(38.5d);
        row[3].Should().Be(4UL);
    }

    [Fact]
    public void Select_1_is_a_uint8_column_named_1()
    {
        var result = _engine.Execute("SELECT 1", NoParams);

        result.Columns.Should().ContainSingle().Which.Should().Be(new ClickHouseColumn("1", "UInt8"));
        result.Rows.Single()[0].Should().Be((byte)1);
    }

    [Fact]
    public void Null_values_make_the_column_nullable()
    {
        var result = _engine.Execute("SELECT NULL AS n, 'x' AS s", NoParams);

        result.Columns[0].TypeString.Should().Be("Nullable(Int32)");
        result.Columns[1].TypeString.Should().Be("String");
        result.Rows.Single()[0].Should().Be(DBNull.Value);
    }

    [Theory]
    [InlineData("SELEC 1", 62, "SYNTAX_ERROR")]
    [InlineData("SELECT * FROM nope", 60, "UNKNOWN_TABLE")]
    [InlineData("SELECT BAD SYNTAX", 47, "UNKNOWN_IDENTIFIER")]
    [InlineData("SELECT * FROM order_timings FINAL", 48, "NOT_IMPLEMENTED")]
    public void Duckdb_and_translator_errors_map_to_clickhouse_error_codes(string sql, int code, string name)
    {
        var act = () => _engine.Execute(sql, NoParams);

        var exception = act.Should().Throw<ClickHouseEmulatorException>().Which;
        exception.Code.Should().Be(code);
        exception.Name.Should().Be(name);
    }

    [Fact]
    public void Summing_an_integer_column_is_refused_rather_than_mis_encoded()
    {
        _engine.ExecuteDdl("CREATE TABLE ints (n Int64) ENGINE = MergeTree() ORDER BY n");
        _engine.ExecuteNonQuery("INSERT INTO ints (n) VALUES ({n:Int64})", Params(("n", "1")));

        var act = () => _engine.Execute("SELECT sum(n) FROM ints", NoParams);

        act.Should().Throw<ClickHouseEmulatorException>().WithMessage("*never sum() an integer column*");
    }

    [Fact]
    public void Concurrent_callers_are_serialised_safely()
    {
        var station = $"Par-{Guid.NewGuid():N}";

        Parallel.For(0, 50, i => _engine.ExecuteNonQuery(
            "INSERT INTO order_timings (timing_id, order_id, station, item_type, prep_seconds, recorded_at) " +
            "VALUES ({timingId:String}, {orderId:String}, {station:String}, {itemType:String}, {prepSeconds:Float64}, {recordedAt:DateTime})",
            Params(("timingId", i.ToString()), ("orderId", "o"), ("station", station), ("itemType", "x"), ("prepSeconds", "1"), ("recordedAt", "2026-09-02T10:11:12"))));

        var count = _engine.Execute("SELECT count() FROM order_timings WHERE station = {s:String}", Params(("s", station)));
        count.Rows.Single()[0].Should().Be(50UL);
    }
}
