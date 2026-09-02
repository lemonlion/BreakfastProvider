using InMemoryEmulator.ClickHouse.Core;

namespace BreakfastProvider.Tests.Unit.Emulator;

public class ClickHouseSqlTranslatorTests
{
    private readonly ClickHouseSqlTranslator _translator = new("kitchen_analytics");

    [Fact]
    public void Rule_1_create_database_is_a_no_op()
    {
        _translator.TranslateDdl("CREATE DATABASE IF NOT EXISTS kitchen_analytics;").Should().BeNull();
    }

    [Fact]
    public void Rule_2_engine_order_by_partition_by_ttl_and_settings_are_stripped_from_ddl()
    {
        var ddl = """
                  CREATE TABLE IF NOT EXISTS kitchen_analytics.order_timings
                  (
                      timing_id String,
                      prep_seconds Float64,
                      recorded_at DateTime
                  )
                  ENGINE = MergeTree()
                  PARTITION BY toYYYYMM(recorded_at)
                  ORDER BY (station, recorded_at)
                  TTL recorded_at + INTERVAL 30 DAY
                  SETTINGS index_granularity = 8192;
                  """;

        _translator.TranslateDdl(ddl).Should().Be(
            "CREATE TABLE IF NOT EXISTS order_timings (timing_id VARCHAR, prep_seconds DOUBLE, recorded_at TIMESTAMP)");
    }

    [Fact]
    public void Rule_3_database_qualifier_is_stripped_from_queries()
    {
        _translator.TranslateQuery("SELECT timing_id FROM kitchen_analytics.order_timings")
            .Should().Be("SELECT timing_id FROM order_timings");
    }

    [Theory]
    [InlineData("String", "VARCHAR")]
    [InlineData("Float64", "DOUBLE")]
    [InlineData("DateTime", "TIMESTAMP")]
    [InlineData("UInt32", "UINTEGER")]
    [InlineData("UInt64", "UBIGINT")]
    [InlineData("Int32", "INTEGER")]
    [InlineData("Int64", "BIGINT")]
    [InlineData("Nullable(String)", "VARCHAR")]
    public void Rule_4_ddl_types_map_to_duckdb_types(string clickHouseType, string duckDbType)
    {
        _translator.TranslateDdl($"CREATE TABLE t (c {clickHouseType}) ENGINE = MergeTree() ORDER BY c")
            .Should().Be($"CREATE TABLE t (c {duckDbType})");
    }

    [Fact]
    public void Rule_4_unknown_ddl_type_throws_naming_the_type()
    {
        var act = () => _translator.TranslateDdl("CREATE TABLE t (c DateTime64(3)) ENGINE = MergeTree() ORDER BY c");

        act.Should().Throw<NotSupportedException>().WithMessage("*DateTime64(3)*");
    }

    [Fact]
    public void Rule_5_placeholders_are_left_untouched_for_the_binder()
    {
        _translator.TranslateQuery("SELECT 1 FROM order_timings WHERE station = {station:String}")
            .Should().Be("SELECT 1 FROM order_timings WHERE station = {station:String}");
    }

    [Fact]
    public void Rule_6_quantile_p_x_becomes_quantile_cont_x_p()
    {
        _translator.TranslateQuery("SELECT quantile(0.95)(prep_seconds) AS p95 FROM order_timings")
            .Should().Be("SELECT quantile_cont(prep_seconds, 0.95) AS p95 FROM order_timings");
    }

    [Fact]
    public void Rule_7_count_becomes_an_unsigned_bigint_count_star()
    {
        _translator.TranslateQuery("SELECT station, count() AS timing_count FROM order_timings GROUP BY station")
            .Should().Be("SELECT station, CAST(count(*) AS UBIGINT) AS timing_count FROM order_timings GROUP BY station");
    }

    [Fact]
    public void Rule_8_select_1_is_typed_as_uint8_named_1_like_the_server()
    {
        _translator.TranslateQuery("SELECT 1").Should().Be("SELECT CAST(1 AS UTINYINT) AS \"1\"");
    }

    [Theory]
    [InlineData("DELETE FROM equipment_readings WHERE reading_id = {readingId:String}")]
    [InlineData("INSERT INTO order_timings (timing_id, prep_seconds) VALUES ({timingId:String}, {prepSeconds:Float64})")]
    [InlineData("SELECT station, avg(prep_seconds) AS avg_prep_seconds FROM order_timings GROUP BY station ORDER BY avg_prep_seconds DESC")]
    public void Rule_9_delete_insert_and_select_group_by_order_by_pass_through(string sql)
    {
        _translator.TranslateQuery(sql).Should().Be(sql);
    }

    [Fact]
    public void The_full_feature_summary_statement_translates_as_a_whole()
    {
        var sql = "SELECT station, avg(prep_seconds) AS avg_prep_seconds, quantile(0.95)(prep_seconds) AS p95_prep_seconds, count() AS timing_count " +
                  "FROM order_timings GROUP BY station ORDER BY avg_prep_seconds DESC";

        _translator.TranslateQuery(sql).Should().Be(
            "SELECT station, avg(prep_seconds) AS avg_prep_seconds, quantile_cont(prep_seconds, 0.95) AS p95_prep_seconds, CAST(count(*) AS UBIGINT) AS timing_count " +
            "FROM order_timings GROUP BY station ORDER BY avg_prep_seconds DESC");
    }

    [Theory]
    [InlineData("SELECT * FROM order_timings FINAL", "FINAL")]
    [InlineData("SELECT * FROM order_timings PREWHERE station = 'x'", "PREWHERE")]
    [InlineData("SELECT quantileExact(0.5)(prep_seconds) FROM order_timings", "quantile*")]
    [InlineData("SELECT toDateTime('2026-01-01 00:00:00')", "to*()")]
    [InlineData("OPTIMIZE TABLE order_timings", "OPTIMIZE TABLE")]
    [InlineData("UPDATE order_timings SET station = 'x'", "UPDATE")]
    public void Unrecognised_clickhouse_constructs_throw_naming_the_fragment(string sql, string fragment)
    {
        var act = () => _translator.TranslateQuery(sql);

        act.Should().Throw<NotSupportedException>().WithMessage($"*{fragment}*");
    }

    [Fact]
    public void Text_that_is_not_a_statement_is_a_syntax_error_like_the_server_reports()
    {
        var act = () => _translator.TranslateQuery("SELEC 1");

        act.Should().Throw<ClickHouseEmulatorException>().Which.Code.Should().Be(62);
    }

    [Fact]
    public void Column_definitions_with_extra_clauses_are_refused()
    {
        var act = () => _translator.TranslateDdl("CREATE TABLE t (c String DEFAULT 'x') ENGINE = MergeTree() ORDER BY c");

        act.Should().Throw<NotSupportedException>().WithMessage("*c String DEFAULT 'x'*");
    }

    [Fact]
    public void Ddl_and_result_set_classification_follows_the_first_keyword()
    {
        ClickHouseSqlTranslator.IsDdl("CREATE TABLE t (c String)").Should().BeTrue();
        ClickHouseSqlTranslator.IsDdl("  create database x").Should().BeTrue();
        ClickHouseSqlTranslator.IsDdl("INSERT INTO t VALUES (1)").Should().BeFalse();
        ClickHouseSqlTranslator.ReturnsResultSet("SELECT 1").Should().BeTrue();
        ClickHouseSqlTranslator.ReturnsResultSet("DELETE FROM t WHERE 1").Should().BeFalse();
        ClickHouseSqlTranslator.ReturnsResultSet("INSERT INTO t VALUES (1)").Should().BeFalse();
    }
}
