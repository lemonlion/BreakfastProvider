using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BreakfastProvider.Api.Services.HealthChecks;

public class BigQueryHealthCheck(BigQueryClient? bigQueryClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (bigQueryClient is null)
            return HealthCheckResult.Unhealthy("BigQuery client is not configured.");

        try
        {
            await bigQueryClient.ListDatasetsAsync().ReadPageAsync(1);
            return HealthCheckResult.Healthy("BigQuery is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("BigQuery is not reachable.", ex);
        }
    }
}
