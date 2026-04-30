using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BreakfastProvider.Api.Telemetry;

public static class DiagnosticsConfig
{
    public const string ServiceName = "BreakfastProvider.Api";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    // Counters
    public static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>(
        "breakfast.orders.created", description: "Number of orders created");

    public static readonly Counter<long> OrderStatusChanged = Meter.CreateCounter<long>(
        "breakfast.orders.status_changed", description: "Number of order status transitions");

    public static readonly Counter<long> RecipesLogged = Meter.CreateCounter<long>(
        "breakfast.recipes.logged", description: "Number of recipes logged");

    public static readonly Counter<long> OutboxMessagesDispatched = Meter.CreateCounter<long>(
        "breakfast.outbox.messages_dispatched", description: "Number of outbox messages successfully dispatched");

    public static readonly Counter<long> OutboxMessagesFailed = Meter.CreateCounter<long>(
        "breakfast.outbox.messages_failed", description: "Number of outbox messages that failed permanently");

    public static readonly Counter<long> CacheHits = Meter.CreateCounter<long>(
        "breakfast.cache.hits", description: "Number of cache hits");

    public static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>(
        "breakfast.cache.misses", description: "Number of cache misses");

    // Histograms
    public static readonly Histogram<double> KafkaPublishDuration = Meter.CreateHistogram<double>(
        "breakfast.kafka.publish.duration", unit: "ms", description: "Duration of Kafka publish operations");

    public static readonly Histogram<double> PubSubPublishDuration = Meter.CreateHistogram<double>(
        "breakfast.pubsub.publish.duration", unit: "ms", description: "Duration of Pub/Sub publish operations");

    public static readonly Histogram<double> EventHubPublishDuration = Meter.CreateHistogram<double>(
        "breakfast.eventhub.publish.duration", unit: "ms", description: "Duration of Event Hub publish operations");
}
