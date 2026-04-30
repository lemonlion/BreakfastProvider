using BreakfastProvider.Api.Data.Spanner;
using Google.Cloud.Spanner.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BreakfastProvider.Api.Services.HealthChecks;

public class SpannerHealthCheck(ISpannerConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var cmd = connection.CreateSelectCommand("SELECT 1");
            await cmd.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Spanner is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Spanner is not reachable.", ex);
        }
    }
}
