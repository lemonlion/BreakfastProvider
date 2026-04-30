using BreakfastProvider.Api.Data;
using BreakfastProvider.Api.Data.Spanner;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Api.Services;
using BreakfastProvider.Api.Storage;
using Azure.Messaging.EventGrid;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Events.Outbox;
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
using Spanner.InMemoryEmulator;
using TestTrackingDiagrams.Extensions;
using TestTrackingDiagrams.Extensions.CosmosDB;
using TestTrackingDiagrams.Extensions.EfCore.Relational;
using TestTrackingDiagrams.Extensions.Grpc;
using TestTrackingDiagrams.Extensions.Kafka;
using TestTrackingDiagrams.Extensions.Spanner;
using TestTrackingDiagrams.Tracking;

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
                    fakeHandler)));

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
    /// instances from the TestTrackingDiagrams.Extensions.Kafka package so that Kafka
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
        // direction). CallerDependencyCategory = "MessageQueue" renders the
        // broker as a queue shape. DependencyCategory = "" keeps BP as entity.
        services.AddHttpContextAccessor();
        services.AddKeyedSingleton("Kafka", (sp, _) => new MessageTracker(
            new MessageTrackerOptions
            {
                CallerName = Documentation.ServiceNames.KafkaBroker,
                ServiceName = Documentation.ServiceNames.BreakfastProvider,
                Verbosity = MessageTrackerVerbosity.Detailed,
                UseHttpContextCorrelation = true,
                CurrentTestInfoFetcher = currentTestInfoFetcher,
                CallerDependencyCategory = "MessageQueue",
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
                CallerDependencyCategory = "MessageQueue",
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
                CallerDependencyCategory = "MessageQueue",
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
    /// Replaces the production <see cref="BreakfastProvider.Api.Grpc.NotificationGrpc.NotificationGrpcClient"/>
    /// with a tracked version that routes calls to a fake notification gRPC service
    /// running in-process via the TestServer handler and records all calls for PlantUML
    /// sequence diagrams. Uses <see cref="GrpcResponseVersionHandler"/> to fix the
    /// HTTP response version (TestServer returns HTTP/1.1, gRPC requires HTTP/2).
    /// </summary>
    public static IServiceCollection UseTrackedGrpcNotificationClient(this IServiceCollection services,
        Func<(string Name, string Id)> currentTestInfoFetcher,
        HttpMessageHandler testServerHandler)
    {
        services.RemoveAll<Api.Grpc.NotificationGrpc.NotificationGrpcClient>();

        services.AddTrackedGrpcClient<Api.Grpc.NotificationGrpc.NotificationGrpcClient>(
            new GrpcResponseVersionHandler(testServerHandler),
            new Uri("http://localhost"),
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
}
