using BreakfastProvider.Api.Configuration;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Services.HealthChecks;

public class PubSubHealthCheck(IOptions<PubSubConfig> pubSubConfig) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var config = pubSubConfig.Value;

        if (string.IsNullOrWhiteSpace(config.ProjectId))
            return HealthCheckResult.Healthy("Pub/Sub not configured.");

        try
        {
            var publisherApi = await PublisherServiceApiClient.CreateAsync(cancellationToken);
            var topicName = new TopicName(config.ProjectId, config.PublisherConfigurations.Values.First().TopicId);
            await publisherApi.GetTopicAsync(topicName, cancellationToken);

            return HealthCheckResult.Healthy($"Pub/Sub topic '{topicName}' is reachable.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Pub/Sub is unreachable.",
                ex);
        }
    }
}
