using System.Data.Common;

namespace BreakfastProvider.Api.Data.ClickHouse;

/// <summary>
/// Creates ClickHouse connections. The return type is deliberately <see cref="DbConnection"/> rather than the
/// driver's concrete connection type so that tests can substitute a tracking decorator without any change to
/// the services, which code against ADO.NET abstractions only.
/// </summary>
public interface IClickHouseConnectionFactory
{
    DbConnection CreateConnection();
}
