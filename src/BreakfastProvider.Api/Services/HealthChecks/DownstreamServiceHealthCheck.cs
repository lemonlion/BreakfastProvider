using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BreakfastProvider.Api.Services.HealthChecks;

public class DownstreamServiceHealthCheck(
    IHttpClientFactory httpClientFactory,
    string clientName,
    string healthEndpoint) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(clientName);
            using var response = await client.GetAsync(healthEndpoint, cancellationToken);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy($"{clientName} is reachable.");

            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"{clientName} returned status code {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"{clientName} is unreachable.",
                ex);
        }
    }
}
