using System.Data.Common;
using ClickHouse.Client.ADO;

namespace BreakfastProvider.Api.Data.ClickHouse;

public class ClickHouseConnectionFactory(string connectionString) : IClickHouseConnectionFactory
{
    public DbConnection CreateConnection() => new ClickHouseConnection(connectionString);
}
