using System.Data.Common;

namespace BreakfastProvider.Api.Data.ClickHouse;

/// <summary>
/// Registered when ClickHouse is not configured (empty connection string).
/// Any call to CreateConnection will throw so that misconfiguration surfaces
/// as a clear error instead of a connection failure deep inside the driver.
/// </summary>
public class NoOpClickHouseConnectionFactory : IClickHouseConnectionFactory
{
    public DbConnection CreateConnection() =>
        throw new InvalidOperationException("ClickHouse is not configured. Set ClickHouseConfig__ConnectionString to use ClickHouse features.");
}
