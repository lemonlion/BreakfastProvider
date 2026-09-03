using System.Text;
using InMemoryEmulator.ClickHouse.Http;
using static BreakfastProvider.Tests.Unit.Emulator.TestSupport;

namespace BreakfastProvider.Tests.Unit.Emulator;

/// <summary>
/// The oracle for the encoder is the driver itself: encode, hand the bytes to a real
/// <c>ClickHouseConnection</c> over a stub handler, and assert the materialised CLR values.
/// Hand-written bytes only for the varint 127/128 boundary and the Nullable flag.
/// </summary>
public class RowBinaryWriterTests
{
    private static readonly DateTime SampleUtc = new(2026, 9, 2, 10, 11, 12, DateTimeKind.Utc);

    [Fact]
    public async Task Driver_reads_back_every_supported_type_with_the_right_clr_type()
    {
        var resultSet = ResultSet(
            [("timing_id", "String"), ("prep_seconds", "Float64"), ("recorded_at", "DateTime"), ("cnt", "UInt64"),
             ("note", "Nullable(String)"), ("flag", "UInt8"), ("small", "Int32"), ("big", "Int64"), ("uint", "UInt32"), ("single", "Float32")],
            ["id-1", 2.5d, SampleUtc, 3UL, "hello", (byte)1, -7, -9L, 5U, 1.25f],
            ["id-2", 0.25d, SampleUtc.AddMinutes(1), ulong.MaxValue, DBNull.Value, true, int.MaxValue, long.MinValue, uint.MaxValue, -0.5f]);

        var bytes = RowBinaryWriter.WriteWithNamesAndTypes(resultSet);

        await using var connection = StubClickHouseHandler.Connect(bytes, out _);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT anything";
        await using var reader = await command.ExecuteReaderAsync();

        reader.FieldCount.Should().Be(10);
        reader.GetName(0).Should().Be("timing_id");
        reader.GetFieldType(0).Should().Be(typeof(string));
        reader.GetFieldType(1).Should().Be(typeof(double));
        reader.GetFieldType(2).Should().Be(typeof(DateTime));
        reader.GetFieldType(3).Should().Be(typeof(ulong));
        reader.GetFieldType(5).Should().Be(typeof(byte));
        reader.GetFieldType(6).Should().Be(typeof(int));
        reader.GetFieldType(7).Should().Be(typeof(long));
        reader.GetFieldType(8).Should().Be(typeof(uint));
        reader.GetFieldType(9).Should().Be(typeof(float));

        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetString(0).Should().Be("id-1");
        reader.GetDouble(1).Should().Be(2.5d);
        reader.GetDateTime(2).Should().Be(SampleUtc);
        reader.GetDateTime(2).Kind.Should().Be(DateTimeKind.Unspecified); // ClickHouse.Driver returns zoneless DateTimes as Unspecified
        reader.GetValue(3).Should().Be(3UL);
        reader.GetString(4).Should().Be("hello");
        reader.GetValue(5).Should().Be((byte)1);
        reader.GetInt32(6).Should().Be(-7);
        reader.GetInt64(7).Should().Be(-9L);
        reader.GetValue(8).Should().Be(5U);
        reader.GetFloat(9).Should().Be(1.25f);

        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetString(0).Should().Be("id-2");
        reader.GetValue(3).Should().Be(ulong.MaxValue);
        reader.IsDBNull(4).Should().BeTrue();
        reader.GetValue(5).Should().Be((byte)1, "bool true is encoded as UInt8 1");
        reader.GetInt32(6).Should().Be(int.MaxValue);
        reader.GetInt64(7).Should().Be(long.MinValue);
        reader.GetValue(8).Should().Be(uint.MaxValue);

        (await reader.ReadAsync()).Should().BeFalse();
    }

    [Fact]
    public void Varint_encodes_the_127_128_boundary_as_leb128()
    {
        Varint(0).Should().Equal([0x00]);
        Varint(127).Should().Equal([0x7F]);
        Varint(128).Should().Equal([0x80, 0x01]);
        Varint(300).Should().Equal([0xAC, 0x02]);
    }

    [Fact]
    public void Nullable_writes_a_flag_byte_then_the_value_or_nothing()
    {
        Encode("Nullable(String)", DBNull.Value).Should().Equal([0x01]);
        Encode("Nullable(String)", "a").Should().Equal([0x00, 0x01, (byte)'a']);
        Encode("Nullable(UInt8)", (byte)7).Should().Equal([0x00, 0x07]);
    }

    [Fact]
    public void Fixed_width_values_are_little_endian_and_datetime_is_unix_seconds()
    {
        Encode("UInt32", 1U).Should().Equal([0x01, 0x00, 0x00, 0x00]);
        Encode("Int64", -1L).Should().Equal(Enumerable.Repeat((byte)0xFF, 8));
        Encode("Float64", 1.0d).Should().Equal(BitConverter.GetBytes(1.0d));
        Encode("DateTime", SampleUtc).Should().Equal(BitConverter.GetBytes((uint)new DateTimeOffset(SampleUtc).ToUnixTimeSeconds()));
        Encode("String", "hé").Should().Equal([0x03, 0x68, 0xC3, 0xA9]);
    }

    [Fact]
    public void Header_is_column_count_then_names_then_types()
    {
        var bytes = RowBinaryWriter.WriteWithNamesAndTypes(ResultSet([("a", "UInt8"), ("bb", "String")]));

        bytes.Should().Equal([0x02, 0x01, (byte)'a', 0x02, (byte)'b', (byte)'b', 0x05, .. "UInt8"u8.ToArray(), 0x06, .. "String"u8.ToArray()]);
    }

    [Fact]
    public void Null_in_a_non_nullable_column_is_refused()
    {
        var act = () => Encode("String", DBNull.Value);

        act.Should().Throw<InvalidOperationException>().WithMessage("*non-Nullable*");
    }

    private static byte[] Varint(ulong value)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            RowBinaryWriter.WriteVarint(writer, value);
        return stream.ToArray();
    }

    private static byte[] Encode(string type, object? value)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            RowBinaryWriter.WriteValue(writer, type, value);
        return stream.ToArray();
    }
}
