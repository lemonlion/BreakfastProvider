using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BreakfastProvider.Api.Services.HealthChecks;

public class NoOpHealthCheck : IHealthCheck
{
    private readonly HealthCheckResult _result;

    public NoOpHealthCheck(string description)
        => _result = HealthCheckResult.Healthy(description);

    public NoOpHealthCheck(HealthCheckResult result)
        => _result = result;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(_result);
}
