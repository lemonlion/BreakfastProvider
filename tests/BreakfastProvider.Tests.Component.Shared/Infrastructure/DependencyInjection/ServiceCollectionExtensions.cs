using System.Data.Common;
using BreakfastProvider.Api.Data;
using BreakfastProvider.Api.Data.ClickHouse;
using BreakfastProvider.Api.Data.Spanner;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Api.Services;
using BreakfastProvider.Api.Storage;
using Azure.Messaging.EventGrid;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Tests.Component.Shared.Fakes.ClickHouse;
using BreakfastProvider.Tests.Component.Shared.Fakes.Cosmos;
using BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;
using BreakfastProvider.Tests.Component.Shared.Fakes.EventHub;
using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;
using BreakfastProvider.Tests.Component.Shared.Fakes.Tracking;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using CosmosDB.InMemoryEmulator;
using InMemoryEmulator.MongoDB;
using InMemoryEmulator.BigQuery;
using Spanner.InMemoryEmulator;
using Kronikol.Constants;
using Kronikol.Extensions;
using Kronikol.Extensions.CosmosDB;
using Kronikol.Extensions.EfCore.Relational;
using Kronikol.Extensions.Grpc;
using Kronikol.Extensions.Kafka;
using Kronikol.Extensions.MongoDB;
using Kronikol.Extensions.BigQuery;
using Kronikol.Extensions.ClickHouse;
using Kronikol.Extensions.Spanner;
using Kronikol.Tracking;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseInMemoryDatabase(this IServiceCollection services, Func<(string Name, string Id)> _currentTestInfoFetcher)
    {
        services.UseInMemoryCosmosDB(options => options
            .AddContainer("orders", "/partitionKey")
            .WithHttpMessageHandlerWrapper(fakeHandler =>
                new CosmosTrackingMessageHandler(
                    new CosmosTrackingMessageHandlerOptions
                    {
                        ServiceName = Documentation.ServiceNames.CosmosDb,
                        CallerName = Documentation.ServiceNames.BreakfastProvider,
                        Verbosity = CosmosTrackingVerbosity.Summarised,
                        CurrentTestInfoFetcher = _currentTestInfoFetcher
                    },
                    fakeHandler,
                    new HttpContextAccessor())));

        return services;
    }

    /// <summary>
    /// Replaces the production <see cref="Microsoft.Azure.Cosmos.CosmosClient"/> with a tracked
    /// version that uses <c>CosmosClientOptions.WithTestTrackingAndCustomSslValidation()</c>.
    /// Use in Docker mode where the real Cosmos emulator accepts HTTP requests over self-signed TLS.
    /// </summary>
    public static IServiceCollection UseTrackedCosmosClient(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var trackingOptions = new CosmosTrackingMessageHandlerOptions
        {
            ServiceName = Documentation.ServiceNames.CosmosDb,
            CallerName = Documentation.ServiceNames.BreakfastProvider,
            Verbosity = CosmosTrackingVerbosity.Summarised,
            CurrentTestInfoFetcher = currentTestInfoFetcher
        };

        // Remove the production CosmosClient and re-register with tracking wired in
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Microsoft.Azure.Cosmos.CosmosClient));
        if (existingDescriptor is not null)
        {
            services.RemoveAll<Microsoft.Azure.Cosmos.CosmosClient>();
            services.AddSingleton(sp =>
            {
                var httpContextAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
                trackingOptions.HttpContextAccessor = httpContextAccessor;
                var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Api.Configuration.CosmosConfig>>().Value;
                var options = new Microsoft.Azure.Cosmos.CosmosClientOptions
                {
                    RequestTimeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds),
                    MaxRetryAttemptsOnRateLimitedRequests = config.MaxRetryAttempts,
                    SerializerOptions = new Microsoft.Azure.Cosmos.CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = Microsoft.Azure.Cosmos.CosmosPropertyNamingPolicy.CamelCase
                    }
                };
                options.WithTestTrackingAndCustomSslValidation(trackingOptions);
                return new Microsoft.Azure.Cosmos.CosmosClient(config.ConnectionString, options);
            });
        }

        return services;
    }

    public static IServiceCollection UseInMemoryEventGrid(this IServiceCollection services)
    {
        // Discover which concrete IEventPublisher<T> types the app registered
        // so we can replace them without naming the event DTOs directly.
        var publisherDescriptors = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == typeof(IEventPublisher<>))
            .ToList();

        foreach (var descriptor in publisherDescriptors)
            services.Remove(descriptor);

        services.RemoveAll<EventGridPublisherClient>();

        // Shared store that all InMemoryEventGridPublisher<T> instances write to.
        var store = new InMemoryEventGridPublisherStore();
        services.AddSingleton(store);
        services.AddSingleton<IPublishedEventStore>(store);

        // Re-register each discovered event type with an in-memory publisher
        // backed by the shared store.
        foreach (var descriptor in publisherDescriptors)
        {
            var eventType = descriptor.ServiceType.GetGenericArguments()[0];
            var publisherType = typeof(InMemoryEventGridPublisher<>).MakeGenericType(eventType);
            services.AddSingleton(descriptor.ServiceType, sp =>
                ActivatorUtilities.CreateInstance(sp, publisherType));
        }

        // Replace the outbox EventGrid dispatcher with an in-memory version
        // so outbox-dispatched events flow into the same shared store.
        services.RemoveAll<IOutboxDispatcher>();
        services.AddSingleton<IOutboxDispatcher>(
            _ => new InMemoryEventGridOutboxDispatcher(store));

        return services;
    }

    /// <summary>
    /// Injects <see cref="EventGridPublisherClientOptions"/> configured with a shared
    /// <see cref="System.Net.Http.SocketsHttpHandler"/> that trusts self-signed
    /// certificates. The shared handler pools TLS connections to the Docker EventGrid
    /// simulator, preventing concurrent handshake contention under parallel tests.
    /// </summary>
    public static IServiceCollection UseSelfSignedEventGridCertificate(this IServiceCollection services)
    {
        services.RemoveAll<EventGridPublisherClient>();
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Api.Configuration.EventGridConfig>>().Value;
            var options = EventGridPublisherClientFactory.CreateOptions();
            return new EventGridPublisherClient(
                new Uri(config.Endpoint),
                new Azure.AzureKeyCredential(config.TopicKey),
                options);
        });

        return services;
    }

    public static IServiceCollection UseInMemoryKafkaBroker(this IServiceCollection services,
        ConsumedKafkaMessageStore consumedStore)
    {
        services.RemoveAll<IProducerFactory>();
        services.AddSingleton<IProducerFactory>(
            _ => new InMemoryKafkaProducerFactory(consumedStore));

        return services;
    }

    /// <summary>
    /// Replaces all <see cref="PubSubEventPublisher{T}"/> registrations with
    /// <see cref="InMemoryPubSubEventPublisher{T}"/> instances backed by a shared store.
    /// </summary>
    public static IServiceCollection UseInMemoryPubSub(this IServiceCollection services,
        ConsumedPubSubMessageStore consumedStore)
    {
        // Remove all PubSubEventPublisher<T> and PublisherClient registrations
        var pubSubDescriptors = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == typeof(PubSubEventPublisher<>))
            .ToList();
        foreach (var d in pubSubDescriptors)
            services.Remove(d);

        var nonGenericPubSub = services
            .Where(d => !d.ServiceType.IsGenericType &&
                        d.ServiceType.FullName?.Contains("PubSubEventPublisher") == true)
            .ToList();
        foreach (var d in nonGenericPubSub)
            services.Remove(d);

        services.RemoveAll<Google.Cloud.PubSub.V1.PublisherClient>();

        services.AddSingleton(consumedStore);

        // Discover all IPubSubEvent implementations and register in-memory publishers
        var eventTypes = typeof(IPubSubEvent).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IPubSubEvent).IsAssignableFrom(t));

        foreach (var eventType in eventTypes)
        {
            var publisherType = typeof(PubSubEventPublisher<>).MakeGenericType(eventType);
            var inMemoryType = typeof(InMemoryPubSubEventPublisher<>).MakeGenericType(eventType);
            services.Remove(services.FirstOrDefault(d => d.ServiceType == publisherType)!);
            services.AddSingleton(publisherType, sp =>
                ActivatorUtilities.CreateInstance(sp, inMemoryType));
        }

        return services;
    }

    /// <summary>
    /// Replaces the real <see cref="Api.Services.HealthChecks.PubSubHealthCheck"/>
    /// with a no-op that always returns Healthy. Used in in-memory test mode where
    /// no real Pub/Sub service is available.
    /// </summary>
    public static IServiceCollection ReplacePubSubHealthCheckWithNoOp(this IServiceCollection services)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            var pubSubReg = options.Registrations.FirstOrDefault(r => r.Name == Api.Services.HealthChecks.HealthCheckNames.PubSub);
            if (pubSubReg is not null)
            {
                options.Registrations.Remove(pubSubReg);
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    Api.Services.HealthChecks.HealthCheckNames.PubSub,
                    _ => new Api.Services.HealthChecks.NoOpHealthCheck("Pub/Sub replaced with in-memory fake."),
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                    tags: [Api.Services.HealthChecks.HealthCheckTags.Infrastructure, Api.Services.HealthChecks.HealthCheckTags.Messaging]));
            }
        });
        return services;
    }

    /// <summary>
    /// Replaces a named downstream health check with a no-op that returns Degraded.
    /// Used in component tests that verify degraded health check reporting.
    /// </summary>
    public static IServiceCollection ReplaceHealthCheckWithDegraded(this IServiceCollection services, string checkName, string description)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            var reg = options.Registrations.FirstOrDefault(r => r.Name == checkName);
            if (reg is not null)
            {
                options.Registrations.Remove(reg);
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    checkName,
                    _ => new Api.Services.HealthChecks.NoOpHealthCheck(
                        new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
                            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                            description)),
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                    tags: [Api.Services.HealthChecks.HealthCheckTags.Downstream, Api.Services.HealthChecks.HealthCheckTags.Api]));
            }
        });
        return services;
    }

    /// <summary>
    /// Replaces a named downstream health check with a real <see cref="Api.Services.HealthChecks.DownstreamServiceHealthCheck"/>
    /// pointing at a failing health endpoint. Used to test the non-success status code branch.
    /// </summary>
    public static IServiceCollection ReplaceHealthCheckWithFailingEndpoint(this IServiceCollection services, string checkName, string failingEndpoint)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            var reg = options.Registrations.FirstOrDefault(r => r.Name == checkName);
            if (reg is not null)
            {
                options.Registrations.Remove(reg);
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    checkName,
                    sp => new Api.Services.HealthChecks.DownstreamServiceHealthCheck(
                        sp.GetRequiredService<IHttpClientFactory>(), checkName, failingEndpoint),
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                    tags: [Api.Services.HealthChecks.HealthCheckTags.Downstream, Api.Services.HealthChecks.HealthCheckTags.Api]));
            }
        });
        return services;
    }

    /// <summary>
    /// Replaces the CosmosDb health check with a no-op that always returns Healthy.
    /// Used in in-memory test mode where <c>UseInMemoryDatabase()</c> removes the
    /// real <see cref="Microsoft.Azure.Cosmos.CosmosClient"/> from DI.
    /// </summary>
    public static IServiceCollection ReplaceCosmosDbHealthCheckWithNoOp(this IServiceCollection services)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            var cosmosReg = options.Registrations.FirstOrDefault(r => r.Name == Api.Services.HealthChecks.HealthCheckNames.CosmosDb);
            if (cosmosReg is not null)
            {
                options.Registrations.Remove(cosmosReg);
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    Api.Services.HealthChecks.HealthCheckNames.CosmosDb,
                    _ => new Api.Services.HealthChecks.NoOpHealthCheck("CosmosDb replaced with in-memory database."),
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                    tags: [Api.Services.HealthChecks.HealthCheckTags.Infrastructure, Api.Services.HealthChecks.HealthCheckTags.Database]));
            }
        });
        return services;
    }

    /// <summary>
    /// Replaces the real <see cref="BreakfastProvider.Api.Services.HealthChecks.KafkaHealthCheck"/>
    /// with a no-op that always returns Healthy. Used in in-memory test mode where
    /// no real Kafka broker is available.
    /// </summary>
    public static IServiceCollection ReplaceKafkaHealthCheckWithNoOp(this IServiceCollection services)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            var kafkaReg = options.Registrations.FirstOrDefault(r => r.Name == Api.Services.HealthChecks.HealthCheckNames.Kafka);
            if (kafkaReg is not null)
            {
                options.Registrations.Remove(kafkaReg);
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    Api.Services.HealthChecks.HealthCheckNames.Kafka,
                    _ => new Api.Services.HealthChecks.NoOpHealthCheck("Kafka broker replaced with in-memory fake."),
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                    tags: [Api.Services.HealthChecks.HealthCheckTags.Infrastructure, Api.Services.HealthChecks.HealthCheckTags.Messaging]));
            }
        });
        return services;
    }

    /// <summary>
    /// Registers a <see cref="MessageTracker"/> singleton configured for EventGrid
    /// with <see cref="MessageTrackerOptions.UseHttpContextCorrelation"/> enabled,
    /// and wraps the existing <see cref="IOutboxWriter"/> registration with a
    /// <see cref="TrackedOutboxWriter"/> so that EventGrid-bound outbox writes
    /// appear in the PlantUML sequence diagrams.
    ///
    /// The app publishes events exclusively through the outbox pattern, so tracking
    /// at the <see cref="IOutboxWriter"/> level is the correct interception point —
    /// it runs inside the HTTP request context where test identity headers are available.
    /// </summary>
    public static IServiceCollection UseTrackedOutboxWriter(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        services.TrackMessagesForDiagrams(new MessageTrackerOptions
        {
            CallerName = Documentation.ServiceNames.BreakfastProvider,
            ServiceName = Documentation.ServiceNames.EventGrid,
            UseHttpContextCorrelation = true,
            CurrentTestInfoFetcher = currentTestInfoFetcher,
            Verbosity = MessageTrackerVerbosity.Summarised
        });

        services.DecorateAll<IOutboxWriter>((sp, inner) =>
            new TrackedOutboxWriter(inner, sp.GetRequiredService<MessageTracker>()));

        return services;
    }

    /// <summary>
    /// Wraps the existing <see cref="IProducerFactory"/> registration
    /// with a factory that produces <see cref="TrackingKafkaProducer{TKey, TValue}"/>
    /// instances from the Kronikol.Extensions.Kafka package so that Kafka
    /// event publications appear in the PlantUML sequence diagrams.
    /// Must be called <b>after</b> <see cref="UseInMemoryKafkaBroker"/>.
    /// </summary>
    public static IServiceCollection UseTrackedKafkaProducer(this IServiceCollection services, Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var options = new KafkaTrackingOptions
        {
            ServiceName = Documentation.ServiceNames.KafkaBroker,
            CallerName = Documentation.ServiceNames.BreakfastProvider,
            Verbosity = KafkaTrackingVerbosity.Summarised,
            CurrentTestInfoFetcher = currentTestInfoFetcher
        };

        services.DecorateAll<IProducerFactory>((sp, kafkaProducerFactory) =>
        {
            var tracker = new KafkaTracker(options, sp.GetService<IHttpContextAccessor>());
            return new TrackingKafkaProducerFactory(kafkaProducerFactory, tracker, options);
        });

        return services;
    }

    /// <summary>
    /// Wraps every <see cref="PubSubEventPublisher{T}"/> registration with a
    /// <see cref="TrackedPubSubEventPublisher{T}"/> decorator so that Pub/Sub
    /// event publications appear in the PlantUML sequence diagrams.
    /// Must be called <b>after</b> <see cref="UseInMemoryPubSub"/> and
    /// <see cref="UseTrackedOutboxWriter"/> (which registers the shared
    /// <see cref="MessageTracker"/> resolved by the decorator).
    /// </summary>
    public static IServiceCollection UseTrackedPubSubPublishers(this IServiceCollection services)
    {
        services.DecorateAllOpen(
            typeof(PubSubEventPublisher<>),
            typeof(TrackedPubSubEventPublisher<>));

        return services;
    }

    /// <summary>
    /// Replaces all <see cref="EventHubEventPublisher{T}"/> registrations with
    /// <see cref="InMemoryEventHubEventPublisher{T}"/> backed by the shared store.
    /// </summary>
    public static IServiceCollection UseInMemoryEventHub(this IServiceCollection services,
        ConsumedEventHubMessageStore consumedEventHubStore)
    {
        // Find all registered EventHubEventPublisher<T> types
        var publisherRegistrations = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == typeof(EventHubEventPublisher<>))
            .Select(d => d.ServiceType)
            .Distinct()
            .ToList();

        foreach (var serviceType in publisherRegistrations)
        {
            var eventType = serviceType.GetGenericArguments()[0];
            var inMemoryType = typeof(InMemoryEventHubEventPublisher<>).MakeGenericType(eventType);

            services.RemoveAll(serviceType);
            services.AddSingleton(serviceType, sp =>
                Activator.CreateInstance(inMemoryType, consumedEventHubStore)!);
        }

        // Remove the real consumer hosted service
        var consumerDescriptor = services.FirstOrDefault(d =>
            d.ImplementationType == typeof(EventHubEquipmentAlertConsumerService));
        if (consumerDescriptor is not null)
            services.Remove(consumerDescriptor);

        // Register the in-memory consumer
        services.AddSingleton<IHostedService, InMemoryEventHubEquipmentAlertConsumerService>();

        return services;
    }

    /// <summary>
    /// Re-registers the real EventHub publisher using options from DI.
    /// Needed because <c>AddEventHub()</c> in Program.cs reads config eagerly
    /// before <c>ConfigureAppConfiguration</c> test overrides are applied,
    /// causing it to register a no-op publisher when the production config
    /// has an empty connection string.
    /// </summary>
    public static IServiceCollection UseRealEventHub(this IServiceCollection services)
    {
        services.RemoveAll<EventHubEventPublisher<EquipmentAlertEvent>>();
        services.RemoveAll<Azure.Messaging.EventHubs.Producer.EventHubProducerClient>();

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Api.Configuration.EventHubConfig>>().Value;
            return new Azure.Messaging.EventHubs.Producer.EventHubProducerClient(
                config.ConnectionString, config.EventHubName);
        });

        services.AddSingleton<EventHubEventPublisher<EquipmentAlertEvent>>();

        return services;
    }

    public static IServiceCollection AddTestTypedEventStores(this IServiceCollection services,
        ConsumedKafkaMessageStore consumedStore,
        ConsumedEventHubMessageStore consumedEventHubStore,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        // MessageTracker for Kafka consume tracking — used by
        // InMemoryReportingKafkaConsumerService to record "Consume (Kafka)"
        // arrows in PlantUML diagrams when the SUT consumes events.
        //
        // CallerName = broker so the arrow goes Kafka → BP (delivery
        // direction). CallerDependencyCategory renders the broker as a queue shape.
        // DependencyCategory = "" keeps BP as entity.
        services.AddHttpContextAccessor();
        services.AddKeyedSingleton("Kafka", (sp, _) => new MessageTracker(
            new MessageTrackerOptions
            {
                CallerName = Documentation.ServiceNames.KafkaBroker,
                ServiceName = Documentation.ServiceNames.BreakfastProvider,
                Verbosity = MessageTrackerVerbosity.Detailed,
                UseHttpContextCorrelation = true,
                CurrentTestInfoFetcher = currentTestInfoFetcher,
                CallerDependencyCategory = DependencyCategories.MessageQueue,
                DependencyCategory = ""
            },
            sp.GetRequiredService<IHttpContextAccessor>()));

        // MessageTracker for Pub/Sub consume tracking — used by
        // InMemoryPubSubBatchCompletionConsumerService to record "Consume (Pub/Sub)"
        // arrows in PlantUML diagrams when the SUT consumes events.
        services.AddKeyedSingleton("PubSub", (sp, _) => new MessageTracker(
            new MessageTrackerOptions
            {
                CallerName = Documentation.ServiceNames.GoogleCloudPubSub,
                ServiceName = Documentation.ServiceNames.BreakfastProvider,
                Verbosity = MessageTrackerVerbosity.Detailed,
                UseHttpContextCorrelation = true,
                CurrentTestInfoFetcher = currentTestInfoFetcher,
                CallerDependencyCategory = DependencyCategories.MessageQueue,
                DependencyCategory = ""
            },
            sp.GetRequiredService<IHttpContextAccessor>()));

        services.AddSingleton<IKafkaMessageStore>(
            _ => new KafkaMessageStore(consumedStore, "RecipeLogEvent"));

        // MessageTracker for Event Hub consume tracking — used by
        // InMemoryEventHubEquipmentAlertConsumerService to record "Consume (Event Hub)"
        // arrows in PlantUML diagrams when the SUT consumes events.
        services.AddKeyedSingleton("EventHub", (sp, _) => new MessageTracker(
            new MessageTrackerOptions
            {
                CallerName = Documentation.ServiceNames.AzureEventHub,
                ServiceName = Documentation.ServiceNames.BreakfastProvider,
                Verbosity = MessageTrackerVerbosity.Detailed,
                UseHttpContextCorrelation = true,
                CurrentTestInfoFetcher = currentTestInfoFetcher,
                CallerDependencyCategory = DependencyCategories.MessageQueue,
                DependencyCategory = ""
            },
            sp.GetRequiredService<IHttpContextAccessor>()));

        return services;
    }

    /// <summary>
    /// Replaces the SQL Server <see cref="ReportingDbContext"/> with an SQLite in-memory
    /// database and removes the <see cref="ReportingKafkaConsumerService"/> hosted service
    /// (tests ingest directly via <see cref="IReportingIngester"/>).
    /// Uses a custom factory to avoid re-registering EF internal services which would
    /// conflict with the SqlServer provider already registered by <c>AddReporting</c>.
    /// </summary>
    public static IServiceCollection UseInMemoryReportingDatabase(this IServiceCollection services)
    {
        // Use a named shared-cache so that multiple connections (from parallel
        // DbContext factory calls) can access the same in-memory database.
        // Keep one connection open to prevent the DB from being destroyed.
        var keepAliveConnection = new SqliteConnection("DataSource=ReportingDb;Mode=Memory;Cache=Shared");
        keepAliveConnection.Open();
        services.AddSingleton(keepAliveConnection);

        // Remove ALL existing EF registrations for ReportingDbContext to avoid
        // the "multiple database providers" conflict between SqlServer and Sqlite.
        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(IDbContextFactory<ReportingDbContext>) ||
                d.ServiceType == typeof(ReportingDbContext) ||
                d.ServiceType == typeof(DbContextOptions<ReportingDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericArguments().Contains(typeof(ReportingDbContext))))
            .ToList();

        foreach (var d in toRemove)
            services.Remove(d);

        // Build fresh options with Sqlite only — don't use AddPooledDbContextFactory
        // to avoid re-registering conflicting EF internal provider services.
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseSqlite("DataSource=ReportingDb;Mode=Memory;Cache=Shared")
            .AddInterceptors(new SqlTrackingInterceptor(
                new SqlTrackingInterceptorOptions
                {
                    ServiceName = Documentation.ServiceNames.ReportingDatabase,
                    CallerName = Documentation.ServiceNames.BreakfastProvider,
                    Verbosity = SqlTrackingVerbosity.Summarised
                },
                new HttpContextAccessor()))
            .Options;

        services.AddSingleton<IDbContextFactory<ReportingDbContext>>(
            new TestReportingDbContextFactory(options));
        services.AddScoped(
            sp => sp.GetRequiredService<IDbContextFactory<ReportingDbContext>>().CreateDbContext());

        // Ensure schema is created
        using var db = new ReportingDbContext(options);
        db.Database.EnsureCreated();

        // Replace the real Kafka consumer (which needs a broker) with an
        // in-memory variant that subscribes to ConsumedKafkaMessageStore
        // and processes messages synchronously within the HTTP request context.
        // This exercises the same consume→ingest pathway and enables
        // MessageTracker to attribute "Consume (Kafka)" diagram arrows.
        var kafkaConsumerDescriptor = services
            .FirstOrDefault(d => d.ImplementationType == typeof(ReportingKafkaConsumerService));
        if (kafkaConsumerDescriptor is not null)
            services.Remove(kafkaConsumerDescriptor);

        services.AddHostedService<InMemoryReportingKafkaConsumerService>();

        // Replace the real Pub/Sub consumer (which needs a subscription) with an
        // in-memory variant that subscribes to ConsumedPubSubMessageStore
        // and processes messages synchronously within the HTTP request context.
        var pubSubConsumerDescriptor = services
            .FirstOrDefault(d => d.ImplementationType == typeof(PubSubBatchCompletionConsumerService));
        if (pubSubConsumerDescriptor is not null)
            services.Remove(pubSubConsumerDescriptor);

        services.AddHostedService<InMemoryPubSubBatchCompletionConsumerService>();

        // Replace the real customer feedback Pub/Sub consumer with an in-memory variant.
        var feedbackConsumerDescriptor = services
            .FirstOrDefault(d => d.ImplementationType == typeof(PubSubCustomerFeedbackConsumerService));
        if (feedbackConsumerDescriptor is not null)
            services.Remove(feedbackConsumerDescriptor);

        services.AddHostedService<InMemoryCustomerFeedbackConsumerService>();

        // Replace the real recipe cost Kafka consumer with an in-memory variant.
        var recipeCostConsumerDescriptor = services
            .FirstOrDefault(d => d.ImplementationType == typeof(KafkaRecipeCostConsumerService));
        if (recipeCostConsumerDescriptor is not null)
            services.Remove(recipeCostConsumerDescriptor);

        services.AddHostedService<InMemoryRecipeCostConsumerService>();

        return services;
    }

    private class TestReportingDbContextFactory(
        DbContextOptions<ReportingDbContext> options) : IDbContextFactory<ReportingDbContext>
    {
        public ReportingDbContext CreateDbContext() => new(options);
    }

    /// <summary>
    /// Replaces the SQL Server <see cref="BreakfastDbContext"/> with an SQLite in-memory
    /// database. Uses the same pattern as <see cref="UseInMemoryReportingDatabase"/> to
    /// avoid the "multiple database providers" conflict.
    /// </summary>
    public static IServiceCollection UseInMemoryBreakfastDatabase(this IServiceCollection services)
    {
        // Use a named shared-cache so that multiple connections (from parallel
        // DbContext factory calls) can access the same in-memory database.
        // Keep one connection open to prevent the DB from being destroyed.
        var keepAliveConnection = new SqliteConnection("DataSource=BreakfastDb;Mode=Memory;Cache=Shared");
        keepAliveConnection.Open();

        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(IDbContextFactory<BreakfastDbContext>) ||
                d.ServiceType == typeof(BreakfastDbContext) ||
                d.ServiceType == typeof(DbContextOptions<BreakfastDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericArguments().Contains(typeof(BreakfastDbContext))))
            .ToList();

        foreach (var d in toRemove)
            services.Remove(d);

        var options = new DbContextOptionsBuilder<BreakfastDbContext>()
            .UseSqlite("DataSource=BreakfastDb;Mode=Memory;Cache=Shared")
            .AddInterceptors(new SqlTrackingInterceptor(
                new SqlTrackingInterceptorOptions
                {
                    ServiceName = Documentation.ServiceNames.BreakfastDatabase,
                    CallerName = Documentation.ServiceNames.BreakfastProvider,
                    Verbosity = SqlTrackingVerbosity.Summarised
                },
                new HttpContextAccessor()))
            .Options;

        // Keep the connection alive as a singleton so GC doesn't close it
        services.AddSingleton(keepAliveConnection);
        services.AddSingleton<IDbContextFactory<BreakfastDbContext>>(
            new TestBreakfastDbContextFactory(options));
        services.AddScoped(
            sp => sp.GetRequiredService<IDbContextFactory<BreakfastDbContext>>().CreateDbContext());

        using var db = new BreakfastDbContext(options);
        db.Database.EnsureCreated();

        return services;
    }

    private class TestBreakfastDbContextFactory(
        DbContextOptions<BreakfastDbContext> options) : IDbContextFactory<BreakfastDbContext>
    {
        public BreakfastDbContext CreateDbContext() => new(options);
    }

    /// <summary>
    /// Replaces the real <see cref="ISpannerConnectionFactory"/> with one that creates
    /// connections from the provided <see cref="FakeSpannerServer"/> with gRPC-level
    /// interception (Option D) so that all Spanner operations — including Spanner-specific
    /// methods like <c>CreateInsertCommand</c>, <c>CreateSelectCommand</c>, and
    /// <c>CreateInsertOrUpdateCommand</c> — appear as tracked dependencies in the diagrams.
    /// <para>
    /// Uses client-side gRPC interception rather than server-side observation because
    /// the interceptor runs within the app's request pipeline where <c>AsyncLocal</c>
    /// test identity and <c>HttpContext</c> are available. Server-side observation
    /// (Option E) would fail because <see cref="FakeSpannerServer"/> handles requests
    /// on its own gRPC thread pool where neither propagates across the TCP boundary.
    /// </para>
    /// The server must already be started and have DDL applied before calling this method.
    /// </summary>
    public static IServiceCollection UseInMemorySpannerDatabase(this IServiceCollection services,
        FakeSpannerServer server,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var trackingOptions = new SpannerTrackingOptions
        {
            ServiceName = Documentation.ServiceNames.Spanner,
            CallerName = Documentation.ServiceNames.BreakfastProvider,
            Verbosity = SpannerTrackingVerbosity.Raw,
            CurrentTestInfoFetcher = currentTestInfoFetcher,
            ExcludedOperations =
            {
                SpannerOperation.CreateSession,
                SpannerOperation.DeleteSession,
                SpannerOperation.BeginTransaction
            }
        };

        // Use Option D (gRPC interceptor) — runs on the client side within
        // the app's async context where test identity is available.
        // Set SPANNER_EMULATOR_HOST so the SDK's SpannerClientBuilder can
        // connect in EmulatorOnly mode. We must NOT set Host/Port on the
        // builder (as server.ConnectionString does) because the SDK forbids
        // an explicit Endpoint when EmulatorDetection.EmulatorOnly is used
        // alongside the SPANNER_EMULATOR_HOST env var.
        Environment.SetEnvironmentVariable("SPANNER_EMULATOR_HOST", $"localhost:{server.Port}");

        var dataSource = new Google.Cloud.Spanner.Data.SpannerConnectionStringBuilder(server.ConnectionString).DataSource;

        services.RemoveAll<ISpannerConnectionFactory>();
        services.AddSingleton<ISpannerConnectionFactory>(sp =>
        {
            var httpContextAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var builder = new Google.Cloud.Spanner.Data.SpannerConnectionStringBuilder
            {
                DataSource = dataSource,
                EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly
            }.WithTestTracking(trackingOptions, httpContextAccessor);
            return new InMemorySpannerConnectionFactory(builder);
        });

        return services;
    }

    private class InMemorySpannerConnectionFactory(Google.Cloud.Spanner.Data.SpannerConnectionStringBuilder builder) : ISpannerConnectionFactory
    {
        public Google.Cloud.Spanner.Data.SpannerConnection CreateConnection() => new(builder);
    }

    /// <summary>
    /// Replaces the Spanner health check with a no-op that always returns Healthy.
    /// Used in in-memory test mode where <c>UseInMemorySpannerDatabase()</c> replaces
    /// the real Spanner connection.
    /// </summary>
    public static IServiceCollection ReplaceSpannerHealthCheckWithNoOp(this IServiceCollection services)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            var spannerReg = options.Registrations.FirstOrDefault(r => r.Name == Api.Services.HealthChecks.HealthCheckNames.Spanner);
            if (spannerReg is not null)
            {
                options.Registrations.Remove(spannerReg);
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    Api.Services.HealthChecks.HealthCheckNames.Spanner,
                    _ => new Api.Services.HealthChecks.NoOpHealthCheck("Spanner replaced with in-memory emulator."),
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                    tags: [Api.Services.HealthChecks.HealthCheckTags.Infrastructure, Api.Services.HealthChecks.HealthCheckTags.Database]));
            }
        });
        return services;
    }

    /// <summary>
    /// Replaces the production <see cref="ISpannerConnectionFactory"/> with a tracked
    /// version that uses <c>SpannerConnectionStringBuilder.WithTestTracking()</c> to wire
    /// up a gRPC interceptor capturing all Spanner operations for test diagrams.
    /// Use in Docker mode where the real Spanner emulator accepts gRPC requests.
    /// </summary>
    public static IServiceCollection UseTrackedSpannerDatabase(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var trackingOptions = new SpannerTrackingOptions
        {
            ServiceName = Documentation.ServiceNames.Spanner,
            CallerName = Documentation.ServiceNames.BreakfastProvider,
            Verbosity = SpannerTrackingVerbosity.Raw,
            CurrentTestInfoFetcher = currentTestInfoFetcher,
            ExcludedOperations =
            {
                SpannerOperation.CreateSession,
                SpannerOperation.DeleteSession,
                SpannerOperation.BeginTransaction
            }
        };

        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISpannerConnectionFactory));
        if (existingDescriptor is not null)
        {
            services.RemoveAll<ISpannerConnectionFactory>();
            services.AddSingleton<ISpannerConnectionFactory>(sp =>
            {
                var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Api.Configuration.SpannerConfig>>().Value;
                var httpContextAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
                var builder = new Google.Cloud.Spanner.Data.SpannerConnectionStringBuilder(config.ConnectionString)
                {
                    EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly
                }.WithTestTracking(trackingOptions, httpContextAccessor);
                return new InMemorySpannerConnectionFactory(builder);
            });
        }

        return services;
    }

    /// <summary>
    /// Replaces the production <see cref="BreakfastProvider.Api.Grpc.NotificationGrpc.NotificationGrpcClient"/>
    /// with a tracked version that routes calls to a Kestrel-hosted fake notification
    /// gRPC service over real HTTP/2 (h2c) and records all calls for PlantUML sequence
    /// diagrams via a gRPC interceptor.
    /// </summary>
    public static IServiceCollection UseTrackedGrpcNotificationClient(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher,
        string baseUrl)
    {
        services.RemoveAll<Api.Grpc.NotificationGrpc.NotificationGrpcClient>();

        services.AddTrackedGrpcClient<Api.Grpc.NotificationGrpc.NotificationGrpcClient>(
            new SocketsHttpHandler(),
            new Uri(baseUrl),
            opts =>
            {
                opts.ServiceName = Documentation.ServiceNames.NotificationService;
                opts.CallerName = Documentation.ServiceNames.BreakfastProvider;
                opts.Verbosity = GrpcTrackingVerbosity.Detailed;
                opts.CurrentTestInfoFetcher = currentTestInfoFetcher;
                // IHttpContextAccessor is auto-resolved from DI — no manual wiring needed
            });

        return services;
    }

    /// <summary>
    /// Replaces the real <see cref="MongoDB.Driver.IMongoClient"/> with an in-memory
    /// MongoDB emulator backed by <c>InMemoryEmulator.MongoDB</c> and wires up
    /// Kronikol tracking via <c>MongoDbTrackingSubscriber</c>.
    /// </summary>
    public static IServiceCollection UseInMemoryMongoDatabase(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var trackingOptions = new MongoDbTrackingOptions
        {
            ServiceName = Documentation.ServiceNames.MongoDB,
            CallerName = Documentation.ServiceNames.BreakfastProvider,
            Verbosity = MongoDbTrackingVerbosity.Detailed,
            CurrentTestInfoFetcher = currentTestInfoFetcher
        };

        var subscriber = new MongoDbTrackingSubscriber(trackingOptions, new HttpContextAccessor());

        services.UseInMemoryMongoDB(options =>
        {
            options.DatabaseName = "BreakfastDb";
            options.AddCollection<Api.Services.RecipeReviewDocument>("recipe_reviews");
            options.AddCollection<Api.Services.ChefNoteDocument>("chef_notes");
            options.AddCollection<Api.Reporting.CustomerFeedbackAlertDocument>("feedback_alerts");
            options.ClusterConfigurator = builder =>
            {
                builder.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(subscriber.OnCommandStarted);
                builder.Subscribe<MongoDB.Driver.Core.Events.CommandSucceededEvent>(subscriber.OnCommandSucceeded);
                builder.Subscribe<MongoDB.Driver.Core.Events.CommandFailedEvent>(subscriber.OnCommandFailed);
            };
        });

        return services;
    }

    /// <summary>
    /// Replaces the production <see cref="MongoDB.Driver.IMongoClient"/> with a tracked
    /// version that uses <c>MongoDbTrackingSubscriber</c> via the driver's <c>ClusterConfigurator</c>.
    /// Use in Docker mode where the real MongoDB container fires command events.
    /// An <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> is passed to enable
    /// dual-resolution of test identity from both HTTP request headers and <c>TestIdentityScope.Current</c>.
    /// </summary>
    public static IServiceCollection UseTrackedMongoClient(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var trackingOptions = new MongoDbTrackingOptions
        {
            ServiceName = Documentation.ServiceNames.MongoDB,
            CallerName = Documentation.ServiceNames.BreakfastProvider,
            Verbosity = MongoDbTrackingVerbosity.Detailed,
            CurrentTestInfoFetcher = currentTestInfoFetcher
        };

        // Remove the production IMongoClient and re-register with tracking wired in
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDB.Driver.IMongoClient));
        if (existingDescriptor is not null)
        {
            services.RemoveAll<MongoDB.Driver.IMongoClient>();
            services.AddSingleton<MongoDB.Driver.IMongoClient>(sp =>
            {
                var httpContextAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>() ?? new HttpContextAccessor();
                var subscriber = new MongoDbTrackingSubscriber(trackingOptions, httpContextAccessor);
                var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Api.Configuration.MongoDbConfig>>().Value;
                var settings = MongoDB.Driver.MongoClientSettings.FromConnectionString(config.ConnectionString);
                settings.ClusterConfigurator = builder => subscriber.Subscribe(builder);
                return new MongoDB.Driver.MongoClient(settings);
            });
        }

        return services;
    }

    /// <summary>
    /// Replaces the MongoDB health check with a no-op that always returns Healthy.
    /// </summary>
    public static IServiceCollection ReplaceMongoHealthCheckWithNoOp(this IServiceCollection services)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            var mongoReg = options.Registrations.FirstOrDefault(r => r.Name == Api.Services.HealthChecks.HealthCheckNames.MongoDB);
            if (mongoReg is not null)
            {
                options.Registrations.Remove(mongoReg);
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    Api.Services.HealthChecks.HealthCheckNames.MongoDB,
                    _ => new Api.Services.HealthChecks.NoOpHealthCheck("MongoDB replaced with in-memory emulator."),
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                    tags: [Api.Services.HealthChecks.HealthCheckTags.Infrastructure, Api.Services.HealthChecks.HealthCheckTags.Database]));
            }
        });
        return services;
    }

    /// <summary>
    /// Replaces the real <see cref="Google.Cloud.BigQuery.V2.BigQueryClient"/> with an
    /// in-memory BigQuery emulator backed by <c>InMemoryEmulator.BigQuery</c> and wires
    /// up Kronikol tracking via <c>BigQueryTrackingMessageHandler</c>.
    /// </summary>
    public static IServiceCollection UseInMemoryBigQuery(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var trackingOptions = new BigQueryTrackingMessageHandlerOptions
        {
            ServiceName = Documentation.ServiceNames.BigQuery,
            CallerName = Documentation.ServiceNames.BreakfastProvider,
            Verbosity = BigQueryTrackingVerbosity.Detailed,
            CurrentTestInfoFetcher = currentTestInfoFetcher
        };

        services.UseInMemoryBigQuery(options =>
        {
            options.ProjectId = "test-project";
            options.AddDataset("breakfast_analytics", ds =>
            {
                ds.AddTable("ingredient_usage", new Google.Cloud.BigQuery.V2.TableSchemaBuilder
                {
                    { "usage_id", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "ingredient_name", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "quantity_used", Google.Cloud.BigQuery.V2.BigQueryDbType.Float64 },
                    { "unit", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "recipe_name", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "recorded_at", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                }.Build());
                ds.AddTable("recipe_costs", new Google.Cloud.BigQuery.V2.TableSchemaBuilder
                {
                    { "calculation_id", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "recipe_name", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "ingredients", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "total_cost", Google.Cloud.BigQuery.V2.BigQueryDbType.Float64 },
                    { "currency", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "calculated_at", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                }.Build());
                ds.AddTable("ingredient_waste", new Google.Cloud.BigQuery.V2.TableSchemaBuilder
                {
                    { "waste_id", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "ingredient_name", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "quantity_wasted", Google.Cloud.BigQuery.V2.BigQueryDbType.Float64 },
                    { "unit", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "recipe_name", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "reason", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                    { "recorded_at", Google.Cloud.BigQuery.V2.BigQueryDbType.String },
                }.Build());
            });
            options.WithHttpMessageHandlerWrapper(fakeHandler =>
                new BigQueryTrackingMessageHandler(trackingOptions, fakeHandler, new HttpContextAccessor()));
        });

        return services;
    }

    /// <summary>
    /// Replaces the production <see cref="Google.Cloud.BigQuery.V2.BigQueryClient"/> with
    /// a tracked version that uses <c>BigQueryClientBuilder.WithTestTracking()</c> to wire
    /// up <c>BigQueryTrackingMessageHandler</c> in the SDK's HTTP pipeline.
    /// Use in Docker mode where the real BigQuery emulator accepts HTTP requests.
    /// </summary>
    public static IServiceCollection UseTrackedBigQueryClient(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var trackingOptions = new BigQueryTrackingMessageHandlerOptions
        {
            ServiceName = Documentation.ServiceNames.BigQuery,
            CallerName = Documentation.ServiceNames.BreakfastProvider,
            Verbosity = BigQueryTrackingVerbosity.Detailed,
            CurrentTestInfoFetcher = currentTestInfoFetcher
        };

        // Remove the production BigQueryClient and re-register with tracking wired in
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Google.Cloud.BigQuery.V2.BigQueryClient));
        if (existingDescriptor is not null)
        {
            services.RemoveAll<Google.Cloud.BigQuery.V2.BigQueryClient>();
            services.AddSingleton(sp =>
            {
                var httpContextAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
                trackingOptions.HttpContextAccessor = httpContextAccessor;
                var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Api.Configuration.BigQueryConfig>>().Value;
                var builder = new Google.Cloud.BigQuery.V2.BigQueryClientBuilder
                {
                    ProjectId = config.ProjectId,
                    BaseUri = config.EmulatorEndpoint,
                    Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken("emulator")
                };
                return builder.WithTestTracking(trackingOptions).Build();
            });
        }

        return services;
    }

    /// <summary>
    /// Replaces the BigQuery health check with a no-op that always returns Healthy.
    /// </summary>
    public static IServiceCollection ReplaceBigQueryHealthCheckWithNoOp(this IServiceCollection services)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            var bqReg = options.Registrations.FirstOrDefault(r => r.Name == Api.Services.HealthChecks.HealthCheckNames.BigQuery);
            if (bqReg is not null)
            {
                options.Registrations.Remove(bqReg);
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    Api.Services.HealthChecks.HealthCheckNames.BigQuery,
                    _ => new Api.Services.HealthChecks.NoOpHealthCheck("BigQuery replaced with in-memory emulator."),
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                    tags: [Api.Services.HealthChecks.HealthCheckTags.Infrastructure, Api.Services.HealthChecks.HealthCheckTags.Database]));
            }
        });
        return services;
    }

    /// <summary>
    /// Replaces the production <see cref="ReportingDbContext"/> registration with one
    /// that adds a <see cref="SqlTrackingInterceptor"/> so that SQL operations against
    /// the real SQL Server reporting database appear in PlantUML sequence diagrams.
    /// Use in Docker mode where the real SQL Server container accepts connections.
    /// </summary>
    public static IServiceCollection UseTrackedReportingDatabase(this IServiceCollection services)
    {
        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(IDbContextFactory<ReportingDbContext>) ||
                d.ServiceType == typeof(ReportingDbContext) ||
                d.ServiceType == typeof(DbContextOptions<ReportingDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericArguments().Contains(typeof(ReportingDbContext))))
            .ToList();

        foreach (var d in toRemove)
            services.Remove(d);

        services.AddSingleton<IDbContextFactory<ReportingDbContext>>(sp =>
        {
            var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ReportingConfig>>().Value;
            var options = new DbContextOptionsBuilder<ReportingDbContext>()
                .UseSqlServer(config.ConnectionString)
                .AddInterceptors(new SqlTrackingInterceptor(
                    new SqlTrackingInterceptorOptions
                    {
                        ServiceName = Documentation.ServiceNames.ReportingDatabase,
                        CallerName = Documentation.ServiceNames.BreakfastProvider,
                        Verbosity = SqlTrackingVerbosity.Summarised
                    },
                    sp.GetRequiredService<IHttpContextAccessor>()))
                .Options;
            return new TestReportingDbContextFactory(options);
        });

        services.AddScoped(sp =>
            sp.GetRequiredService<IDbContextFactory<ReportingDbContext>>().CreateDbContext());

        return services;
    }

    /// <summary>
    /// Replaces the production <see cref="BreakfastDbContext"/> registration with one
    /// that adds a <see cref="SqlTrackingInterceptor"/> so that SQL operations against
    /// the real SQL Server breakfast database appear in PlantUML sequence diagrams.
    /// Use in Docker mode where the real SQL Server container accepts connections.
    /// </summary>
    public static IServiceCollection UseTrackedBreakfastDatabase(this IServiceCollection services)
    {
        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(IDbContextFactory<BreakfastDbContext>) ||
                d.ServiceType == typeof(BreakfastDbContext) ||
                d.ServiceType == typeof(DbContextOptions<BreakfastDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericArguments().Contains(typeof(BreakfastDbContext))))
            .ToList();

        foreach (var d in toRemove)
            services.Remove(d);

        services.AddSingleton<IDbContextFactory<BreakfastDbContext>>(sp =>
        {
            var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Api.Configuration.DatabaseConfig>>().Value;
            var options = new DbContextOptionsBuilder<BreakfastDbContext>()
                .UseSqlServer(config.ConnectionString)
                .AddInterceptors(new SqlTrackingInterceptor(
                    new SqlTrackingInterceptorOptions
                    {
                        ServiceName = Documentation.ServiceNames.BreakfastDatabase,
                        CallerName = Documentation.ServiceNames.BreakfastProvider,
                        Verbosity = SqlTrackingVerbosity.Summarised
                    },
                    sp.GetRequiredService<IHttpContextAccessor>()))
                .Options;
            return new TestBreakfastDbContextFactory(options);
        });

        services.AddScoped(sp =>
            sp.GetRequiredService<IDbContextFactory<BreakfastDbContext>>().CreateDbContext());

        return services;
    }

    /// <summary>
    /// Wraps the existing <see cref="EventHubEventPublisher{T}"/> registrations with
    /// <see cref="TrackedEventHubEventPublisher{T}"/> decorators so that Event Hub event
    /// publications appear in the PlantUML sequence diagrams.
    /// Use in Docker mode where the real Event Hub (emulator) accepts connections.
    /// </summary>
    public static IServiceCollection UseTrackedEventHubPublisher(this IServiceCollection services)
    {
        services.DecorateAllOpen(
            typeof(EventHubEventPublisher<>),
            typeof(Fakes.EventHub.TrackedEventHubEventPublisher<>));

        return services;
    }

    private static ClickHouseTrackingOptions NewClickHouseTrackingOptions(Func<(string Name, string Id)> currentTestInfoFetcher) => new()
    {
        ServiceName = Documentation.ServiceNames.ClickHouse,
        CallerName = Documentation.ServiceNames.BreakfastProvider,
        Verbosity = Kronikol.Sql.SqlTrackingVerbosityLevel.Detailed,
        LogParameters = true,
        // The target property is Func<(string, string)?> (nullable tuple); there is no delegate
        // variance for value types, so a bare assignment of the fetcher would not compile.
        CurrentTestInfoFetcher = () => currentTestInfoFetcher()
    };

    /// <summary>
    /// Replaces the real <see cref="IClickHouseConnectionFactory"/> with one that creates
    /// <c>ClickHouse.Client</c> connections routed through the process-wide in-memory emulator
    /// (<see cref="SharedInMemoryClickHouse"/>) and wrapped with Kronikol's
    /// <c>TrackingClickHouseConnection</c>, so every ClickHouse statement appears in the diagrams.
    /// The services only see a <see cref="DbConnection"/>, so nothing in <c>src</c> changes.
    /// </summary>
    public static IServiceCollection UseInMemoryClickHouse(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var options = NewClickHouseTrackingOptions(currentTestInfoFetcher);
        var server = SharedInMemoryClickHouse.Server;

        services.RemoveAll<IClickHouseConnectionFactory>();
        services.AddSingleton<IClickHouseConnectionFactory>(sp =>
        {
            // Resolve IHttpContextAccessor from DI in both modes so that HTTP-driven flows are
            // attributed by request header and event-driven flows fall back to TestIdentityScope.
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>() ?? new HttpContextAccessor();
            return new TrackedClickHouseConnectionFactory(server.CreateConnection, options);
        });

        return services;
    }

    /// <summary>
    /// Replaces the production <see cref="IClickHouseConnectionFactory"/> with a tracked version
    /// that connects to the real ClickHouse (Docker) using the connection string from
    /// <see cref="Api.Configuration.ClickHouseConfig"/> and wraps it with Kronikol tracking.
    /// </summary>
    public static IServiceCollection UseTrackedClickHouse(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher)
    {
        var options = NewClickHouseTrackingOptions(currentTestInfoFetcher);

        if (services.All(d => d.ServiceType != typeof(IClickHouseConnectionFactory)))
            return services;

        services.RemoveAll<IClickHouseConnectionFactory>();
        services.AddSingleton<IClickHouseConnectionFactory>(sp =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>() ?? new HttpContextAccessor();
            var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Api.Configuration.ClickHouseConfig>>().Value;
            return new TrackedClickHouseConnectionFactory(
                () => new global::ClickHouse.Client.ADO.ClickHouseConnection(config.ConnectionString), options);
        });

        return services;
    }

    /// <summary>
    /// Replaces the ClickHouse health check with a no-op that always returns Healthy.
    /// Used in in-memory test mode where <c>UseInMemoryClickHouse()</c> replaces the real connection.
    /// </summary>
    public static IServiceCollection ReplaceClickHouseHealthCheckWithNoOp(this IServiceCollection services)
    {
        services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
        {
            var chReg = options.Registrations.FirstOrDefault(r => r.Name == Api.Services.HealthChecks.HealthCheckNames.ClickHouse);
            if (chReg is not null)
            {
                options.Registrations.Remove(chReg);
                options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                    Api.Services.HealthChecks.HealthCheckNames.ClickHouse,
                    _ => new Api.Services.HealthChecks.NoOpHealthCheck("ClickHouse replaced with in-memory emulator."),
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                    tags: [Api.Services.HealthChecks.HealthCheckTags.Infrastructure, Api.Services.HealthChecks.HealthCheckTags.Database]));
            }
        });
        return services;
    }

    /// <summary>
    /// Replaces the real <see cref="KafkaOrderServedConsumerService"/> (which needs a broker) with an
    /// in-memory variant that subscribes to <see cref="ConsumedKafkaMessageStore"/>. Event tests publish
    /// straight into that store, so this is needed in every lane, including Docker.
    /// </summary>
    public static IServiceCollection UseInMemoryOrderServedKafkaConsumer(this IServiceCollection services)
    {
        var realConsumer = services.FirstOrDefault(d => d.ImplementationType == typeof(KafkaOrderServedConsumerService));
        if (realConsumer is not null)
            services.Remove(realConsumer);

        services.AddHostedService<InMemoryOrderServedConsumerService>();

        return services;
    }

    private sealed class TrackedClickHouseConnectionFactory(Func<DbConnection> inner, ClickHouseTrackingOptions options)
        : IClickHouseConnectionFactory
    {
        public DbConnection CreateConnection() => inner().WithClickHouseTestTracking(options);
    }
}
