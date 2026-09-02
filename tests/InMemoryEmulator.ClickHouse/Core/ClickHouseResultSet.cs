namespace InMemoryEmulator.ClickHouse.Core;

/// <summary>
/// A materialised query result. Values are CLR values (<see cref="DBNull"/> for NULL) and the
/// column type strings are ClickHouse type names — this is the lingua franca between the
/// transport-agnostic core and any front-end (HTTP today, native TCP possibly later).
/// </summary>
public sealed record ClickHouseResultSet(IReadOnlyList<ClickHouseColumn> Columns, IReadOnlyList<object?[]> Rows)
{
    public static ClickHouseResultSet Empty { get; } = new([], []);
}
