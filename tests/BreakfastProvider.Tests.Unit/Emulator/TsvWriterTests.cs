using InMemoryEmulator.ClickHouse.Http;
using static BreakfastProvider.Tests.Unit.Emulator.TestSupport;

namespace BreakfastProvider.Tests.Unit.Emulator;

public class TsvWriterTests
{
    [Fact]
    public void Handshake_row_is_tab_separated_with_a_trailing_newline()
    {
        var resultSet = ResultSet([("version()", "String"), ("timezone()", "String")], ["25.8.33.6", "UTC"]);

        TsvWriter.Write(resultSet).Should().Be("25.8.33.6\tUTC\n");
    }

    [Fact]
    public void Names_and_types_header_precedes_the_rows_when_requested()
    {
        var resultSet = ResultSet([("a", "String"), ("b", "UInt8")], ["x", (byte)1], ["y", (byte)0]);

        TsvWriter.Write(resultSet, withNamesAndTypes: true).Should().Be("a\tb\nString\tUInt8\nx\t1\ny\t0\n");
    }

    [Fact]
    public void Values_are_formatted_the_way_clickhouse_formats_them()
    {
        TsvWriter.Format(DBNull.Value).Should().Be("\\N");
        TsvWriter.Format(null).Should().Be("\\N");
        TsvWriter.Format(true).Should().Be("1");
        TsvWriter.Format(2.5d).Should().Be("2.5");
        TsvWriter.Format(new DateTime(2026, 9, 2, 10, 11, 12, DateTimeKind.Utc)).Should().Be("2026-09-02 10:11:12");
        TsvWriter.Format("tab\there\nnew\\slash").Should().Be("tab\\there\\nnew\\\\slash");
    }
}
