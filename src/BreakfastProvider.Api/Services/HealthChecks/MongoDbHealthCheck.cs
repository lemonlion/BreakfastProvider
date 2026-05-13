using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace BreakfastProvider.Api.Services.HealthChecks;

public class MongoDbHealthCheck(IMongoClient? mongoClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (mongoClient is null)
            return HealthCheckResult.Unhealthy("MongoDB client is not configured.");

        try
        {
            var cursor = await mongoClient.ListDatabaseNamesAsync(cancellationToken);
            await cursor.MoveNextAsync(cancellationToken);
            return HealthCheckResult.Healthy("MongoDB is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is not reachable.", ex);
        }
    }
}
