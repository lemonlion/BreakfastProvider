using BreakfastProvider.Api.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Services.HealthChecks;

public class KafkaHealthCheck(IOptions<KafkaConfig> kafkaConfig) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var config = kafkaConfig.Value;

        if (string.IsNullOrWhiteSpace(config.BootstrapServers))
            return Task.FromResult(HealthCheckResult.Healthy("Kafka not configured."));

        try
        {
            using var adminClient = new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = config.BootstrapServers })
                .Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(3));
            return Task.FromResult(
                HealthCheckResult.Healthy($"Kafka broker is reachable. Brokers: {metadata.Brokers.Count}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HealthCheckResult(
                context.Registration.FailureStatus,
                "Kafka broker is unreachable.",
                ex));
        }
    }
}
