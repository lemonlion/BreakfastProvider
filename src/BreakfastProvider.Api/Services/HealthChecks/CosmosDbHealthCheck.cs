using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BreakfastProvider.Api.Services.HealthChecks;

public class CosmosDbHealthCheck(CosmosClient? cosmosClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (cosmosClient is null)
            return HealthCheckResult.Unhealthy("Cosmos DB is not configured.");

        try
        {
            var response = await cosmosClient.ReadAccountAsync();
            return HealthCheckResult.Healthy($"Cosmos DB is reachable. Account: {response.Id}");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Cosmos DB is unreachable.",
                ex);
        }
    }
}
