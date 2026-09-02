namespace InMemoryEmulator.ClickHouse.Core;

/// <summary>A result-set column: its name and its ClickHouse type string (e.g. <c>Float64</c>, <c>Nullable(String)</c>).</summary>
public readonly record struct ClickHouseColumn(string Name, string TypeString);
