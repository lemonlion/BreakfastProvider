using InMemoryEmulator.ClickHouse.Core;
using static BreakfastProvider.Tests.Unit.Emulator.TestSupport;

namespace BreakfastProvider.Tests.Unit.Emulator;

/// <summary>The four rows of the captured wire table: what ClickHouse.Client sends for each CLR value.</summary>
public class ClickHouseParameterBinderTests
{
    [Fact]
    public void String_values_are_bound_as_is()
    {
        var (sql, parameters) = ClickHouseParameterBinder.Bind(
            "SELECT 1 WHERE station = {station:String}", Params(("station", "Griddle-abc")));

        sql.Should().Be("SELECT 1 WHERE station = $station");
        parameters.Should().ContainSingle().Which.Should().Be(new BoundParameter("station", "Griddle-abc"));
    }

    [Fact]
    public void Float64_values_are_parsed_with_the_invariant_culture()
    {
        var (_, parameters) = ClickHouseParameterBinder.Bind("SELECT {min:Float64}", Params(("min", "1.5")));

        parameters.Single().Value.Should().Be(1.5d);
    }

    [Fact]
    public void DateTime_values_use_the_drivers_zoneless_format_and_are_stamped_utc()
    {
        var (_, parameters) = ClickHouseParameterBinder.Bind("SELECT {since:DateTime}", Params(("since", "2026-09-02T10:11:12")));

        var value = parameters.Single().Value.Should().BeOfType<DateTime>().Subject;
        value.Should().Be(new DateTime(2026, 9, 2, 10, 11, 12));
        value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Guid_values_arrive_as_strings_and_stay_strings()
    {
        var guid = "0f8fad5b-d9cb-469f-a165-70867728950e";

        var (_, parameters) = ClickHouseParameterBinder.Bind("SELECT {oid:String}", Params(("oid", guid)));

        parameters.Single().Value.Should().Be(guid);
    }

    [Fact]
    public void Every_placeholder_in_the_statement_is_rewritten_and_bound_once()
    {
        var sql = "INSERT INTO t (a, b, c) VALUES ({a:String}, {b:Float64}, {a:String})";

        var (rewritten, parameters) = ClickHouseParameterBinder.Bind(sql, Params(("a", "x"), ("b", "2"), ("unused", "ignored")));

        rewritten.Should().Be("INSERT INTO t (a, b, c) VALUES ($a, $b, $a)");
        parameters.Select(p => p.Name).Should().Equal("a", "b");
    }

    [Fact]
    public void A_placeholder_without_a_value_is_an_unknown_query_parameter_error()
    {
        var act = () => ClickHouseParameterBinder.Bind("SELECT {missing:String}", NoParams);

        act.Should().Throw<ClickHouseEmulatorException>().Which.Code.Should().Be(456);
    }

    [Theory]
    [InlineData("2026-09-02 10:11:12")]
    [InlineData("2026-09-02T10:11:12.5")]
    [InlineData("1788689472")]
    public void Alternative_datetime_spellings_are_accepted(string raw)
    {
        var value = (DateTime)ClickHouseParameterBinder.Coerce(raw, "DateTime");

        value.Kind.Should().Be(DateTimeKind.Utc);
        value.Year.Should().Be(2026);
    }
}
