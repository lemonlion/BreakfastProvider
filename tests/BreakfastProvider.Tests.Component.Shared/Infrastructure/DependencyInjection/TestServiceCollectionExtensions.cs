using BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;
using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for the test-side <see cref="ServiceCollection"/> (the one
/// built in <see cref="BaseFixture"/>).  These bridge app-side singletons into
/// the test DI container so step classes can resolve shared stores for assertions.
/// </summary>
public static class TestServiceCollectionExtensions
{
    /// <summary>
    /// Shared queue drainer that reads EventGrid events from the Azurite
    /// storage queue where the Docker EventGrid simulator delivers them.
    /// A single instance is used across all test fixtures.
    /// </summary>
    public static EventGridQueueDrainer? SharedQueueDrainer { get; private set; }

    public static void InitQueueDrainer(string connectionString)
    {
        SharedQueueDrainer ??= new EventGridQueueDrainer(connectionString);
    }

    public static IServiceCollection AddEventGridPublishers(
        this IServiceCollection services,
        ComponentTestSettings settings,
        IServiceProvider webApplicationServiceProvider)
    {
        // Resolve the IPublishedEventStore from the app container — in both
        // in-memory and Docker mode this is already registered as a non-generic
        // IPublishedEventStore in ConfigureTestServices.
        var store = webApplicationServiceProvider.GetRequiredService<IPublishedEventStore>();
        services.AddSingleton(store);

        return services;
    }

    public static IServiceCollection AddKafkaMessageStore(
        this IServiceCollection services,
        ConsumedKafkaMessageStore consumedStore)
    {
        services.AddSingleton(_ => consumedStore);

        services.AddSingleton<IKafkaMessageStore>(
            _ => new KafkaMessageStore(consumedStore, "RecipeLogEvent"));

        return services;
    }
}
