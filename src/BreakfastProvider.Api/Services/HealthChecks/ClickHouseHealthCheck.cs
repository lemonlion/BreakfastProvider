using BreakfastProvider.Api.Data.ClickHouse;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BreakfastProvider.Api.Services.HealthChecks;

public class ClickHouseHealthCheck(IClickHouseConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("ClickHouse is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ClickHouse is not reachable.", ex);
        }
    }
}
