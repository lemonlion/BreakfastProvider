namespace InMemoryEmulator.ClickHouse.Core;

/// <summary>
/// An error carrying a ClickHouse error code and name, so that front-ends can reproduce the
/// server's own error responses (e.g. <c>Code: 60. DB::Exception: ... (UNKNOWN_TABLE)</c>).
/// </summary>
public sealed class ClickHouseEmulatorException(int code, string name, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>ClickHouse error code (see <c>ErrorCodes.cpp</c> in the ClickHouse source).</summary>
    public int Code { get; } = code;

    /// <summary>ClickHouse error name, e.g. <c>SYNTAX_ERROR</c>.</summary>
    public string Name { get; } = name;

    public static ClickHouseEmulatorException SyntaxError(string message, Exception? inner = null) => new(62, "SYNTAX_ERROR", message, inner);
    public static ClickHouseEmulatorException UnknownTable(string message, Exception? inner = null) => new(60, "UNKNOWN_TABLE", message, inner);
    public static ClickHouseEmulatorException UnknownIdentifier(string message, Exception? inner = null) => new(47, "UNKNOWN_IDENTIFIER", message, inner);
    public static ClickHouseEmulatorException NotImplemented(string message, Exception? inner = null) => new(48, "NOT_IMPLEMENTED", message, inner);
    public static ClickHouseEmulatorException UnknownQueryParameter(string message) => new(456, "UNKNOWN_QUERY_PARAMETER", message);
    public static ClickHouseEmulatorException Generic(string message, Exception? inner = null) => new(1000, "POCO_EXCEPTION", message, inner);
}
