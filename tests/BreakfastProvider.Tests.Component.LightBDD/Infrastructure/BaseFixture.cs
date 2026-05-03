using System.Collections.Concurrent;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Tests.Component.Shared.Common;
using BreakfastProvider.Tests.Component.Shared.Common.AuditLogs;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerPreferences;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Feedback;
using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Inventory;
using BreakfastProvider.Tests.Component.Shared.Common.Menu;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Common.Reservations;
using BreakfastProvider.Tests.Component.Shared.Common.Staff;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Common.Waffles;
using BreakfastProvider.Tests.Component.Shared.Fakes.HttpFakes;
using BreakfastProvider.Tests.Component.Shared.Fakes.EventHub;
using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;
using BreakfastProvider.Tests.Component.LightBDD.LightBddCustomisations;
using LightBDD.Core.ExecutionContext;
using LightBDD.XUnit3;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spanner.InMemoryEmulator;
using TestTrackingDiagrams.LightBDD;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.LightBDD.Infrastructure;

public abstract class BaseFixture : FeatureFixture, IDisposable, IIgnorable<ComponentTestSettings>
{
    private static WebApplicationFactory<Program>? _staticFactory;
    private static readonly ConcurrentDictionary<string, Lazy<WebApplicationFactory<Program>>> SharedFactoryCache = new();
    private WebApplicationFactory<Program>? _appFactory;
    private WebApplicationFactory<Program>? _sharedFactory;
    private static IServiceProvider? _staticServiceProvider;
    private readonly IServiceProvider _testServiceProvider;
    private readonly bool _delayAppCreation;

    protected HttpClient Client { get; private set; } = null!;
    protected WebApplicationFactory<Program> AppFactory =>
        Settings.RunAgainstExternalServiceUnderTest
            ? throw new InvalidOperationException("AppFactory is not available in post-deployment mode (RunAgainstExternalServiceUnderTest is true). Infrastructure-dependent steps should be guarded with [SkipStepIf].")
            : _sharedFactory ?? _appFactory ?? _staticFactory!;
    protected string RequestId { get; } = Guid.NewGuid().ToString();
    public static ComponentTestSettings Settings { get; private set; } = null!;
    public ComponentTestSettings IgnoreSettings => Settings;
    public static FakeRequestStore FakeRequestStore { get; } = new();
    public static readonly ConsumedKafkaMessageStore ConsumedKafkaMessageStore = new();
    public static readonly ConsumedPubSubMessageStore ConsumedPubSubMessageStore = new();
    public static readonly ConsumedEventHubMessageStore ConsumedEventHubMessageStore = new();
    private static FakeSpannerServer? _fakeSpannerServer;

    static BaseFixture()
    {
        var configuration = new ConfigurationBuilder().GetComponentTestConfiguration();
        Settings = configuration.Get<ComponentTestSettings>()!;
    }

    /// <summary>
    /// Eagerly builds the TestServer host so that all parallel test instances share
    /// the same server and singleton services. Must be called AFTER the fake HTTP
    /// services are running but BEFORE any parallel test instances call constructors.
    /// </summary>
    internal static async Task EnsureHostInitialized()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        if (Settings.RunWithAnInMemorySpannerDatabase && _fakeSpannerServer is null)
        {
            _fakeSpannerServer = new FakeSpannerServer();
            _fakeSpannerServer.Start();
            _fakeSpannerServer.Database.ExecuteDdl("CREATE TABLE Feedback (FeedbackId STRING(MAX) NOT NULL, CustomerName STRING(MAX), OrderId STRING(MAX), Rating INT64, Comment STRING(MAX), CreatedAt TIMESTAMP) PRIMARY KEY (FeedbackId)");
            _fakeSpannerServer.Database.ExecuteDdl("CREATE TABLE CustomerPreferences (CustomerId STRING(MAX) NOT NULL, CustomerName STRING(MAX), PreferredMilkType STRING(MAX), LikesExtraToppings BOOL, FavouriteItem STRING(MAX), UpdatedAt TIMESTAMP) PRIMARY KEY (CustomerId)");
        }

        EnsureStaticFactoryCreated();
        _ = _staticFactory!.Services;
        await Task.CompletedTask;
    }

    protected BaseFixture(bool delayAppCreation = false)
    {
        _delayAppCreation = delayAppCreation;

        if (!delayAppCreation)
        {
            if (!Settings.RunAgainstExternalServiceUnderTest)
                EnsureStaticFactoryCreated();
            Client = CreateTestClient();
        }

        var services = new ServiceCollection();
        services.AddSingleton(Settings);
        services.AddSingleton(FakeRequestStore);
        services.AddSingleton(new RequestContext(() => Client, RequestId));

        // Step classes — transient so each Get<T>() returns a fresh instance
        services.AddTransient<GetMilkSteps>();
        services.AddTransient<GetEggsSteps>();
        services.AddTransient<GetFlourSteps>();
        services.AddTransient<GetGoatMilkSteps>();
        services.AddTransient<PostPancakesSteps>();
        services.AddTransient<PostWafflesSteps>();
        services.AddTransient<PostOrderSteps>();
        services.AddTransient<GetOrderSteps>();
        services.AddTransient<ListOrdersSteps>();
        services.AddTransient<PostToppingsSteps>();
        services.AddTransient<GetToppingsSteps>();
        services.AddTransient<GetMenuSteps>();
        services.AddTransient<GetAuditLogsSteps>();
        services.AddTransient<DownstreamRequestSteps>();
        services.AddTransient<OutboxSteps>();
        services.AddTransient<PatchOrderStatusSteps>();
        services.AddTransient<DeleteToppingSteps>();
        services.AddTransient<PutToppingSteps>();
        services.AddTransient<GetDailySpecialsSteps>();
        services.AddTransient<PostDailySpecialOrderSteps>();
        services.AddTransient<ResetDailySpecialOrdersSteps>();
        services.AddTransient<GraphQlReportingSteps>();
        services.AddTransient<PostInventorySteps>();
        services.AddTransient<GetInventorySteps>();
        services.AddTransient<PutInventorySteps>();
        services.AddTransient<DeleteInventorySteps>();
        services.AddTransient<PostStaffSteps>();
        services.AddTransient<GetStaffSteps>();
        services.AddTransient<PostReservationSteps>();
        services.AddTransient<GetReservationSteps>();
        services.AddTransient<CancelReservationSteps>();
        services.AddTransient<PostFeedbackSteps>();
        services.AddTransient<GetFeedbackSteps>();
        services.AddTransient<PutCustomerPreferenceSteps>();
        services.AddTransient<GetCustomerPreferenceSteps>();
        services.AddTransient<GrpcBreakfastSteps>();

        if (!delayAppCreation && !Settings.RunAgainstExternalServiceUnderTest)
        {
            services.AddEventGridPublishers(Settings, AppFactory.Services);
            services.AddKafkaMessageStore(ConsumedKafkaMessageStore);
            services.AddSingleton(AppFactory.Services.GetRequiredService<ICosmosRepository<OutboxMessage>>());
        }

        _testServiceProvider = services.BuildServiceProvider();
    }

    protected void CreateAppAndClient(Dictionary<string, string?>? configOverrides = null, Action<IServiceCollection>? additionalServices = null)
    {
        if ((configOverrides != null && configOverrides.Count > 0) || additionalServices != null)
        {
            _appFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.componenttests.json", optional: true, reloadOnChange: false);
                    if (configOverrides != null)
                        config.AddInMemoryCollection(configOverrides);
                });
                builder.ConfigureTestServices(services =>
                {
                    ConfigureTestServices(services);
                    additionalServices?.Invoke(services);
                });
            });
        }
        else
        {
            EnsureStaticFactoryCreated();
        }

        Client = CreateTestClient();
    }

    /// <summary>
    /// Creates or reuses a shared <see cref="WebApplicationFactory{TEntryPoint}"/> for the given
    /// config overrides. In Docker mode (real Cosmos emulator), all scenarios within a feature
    /// class that pass identical overrides share a single factory, eliminating redundant host
    /// startups and Cosmos emulator contention when <c>AllowTestParallelization</c> runs
    /// scenarios concurrently. In in-memory mode, each scenario gets its own factory to
    /// maintain per-scenario data isolation (InMemoryContainers, event publishers, etc.).
    /// </summary>
    protected void CreateAppAndClientWithSharedFactory(Dictionary<string, string?> settingOverrides)
    {
        // In-memory mode: singletons like InMemoryContainer and InMemoryEventGridPublisher
        // are scoped to the factory. Sharing a factory across parallel scenarios would cause
        // cross-scenario event/data interference. Fall back to per-scenario factories.
        if (Settings.RunWithAnInMemoryDatabase)
        {
            CreateAppAndClient(settingOverrides);
            return;
        }

        var cacheKey = string.Join("|", settingOverrides
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}"));

        var lazyFactory = SharedFactoryCache.GetOrAdd(cacheKey,
            _ => new Lazy<WebApplicationFactory<Program>>(
                () => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.SetBasePath(AppContext.BaseDirectory)
                            .AddJsonFile("appsettings.componenttests.json", optional: true, reloadOnChange: false);
                        config.AddInMemoryCollection(settingOverrides);
                    });
                    builder.ConfigureTestServices(ConfigureTestServices);
                }),
                LazyThreadSafetyMode.ExecutionAndPublication));

        _sharedFactory = lazyFactory.Value;
        Client = CreateTestClient();
    }

    private static void EnsureStaticFactoryCreated()
    {
        if (_staticFactory != null) return;

        _staticFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.componenttests.json", optional: true, reloadOnChange: false);
            });
            builder.ConfigureTestServices(ConfigureTestServices);
        });
        _staticServiceProvider = _staticFactory.Services;
    }

    private static void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IHttpClientFactory>(sp =>
            new TestHttpClientFactory(
                sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                FakeRequestStore,
                Settings));
        services.AddHttpContextAccessor();

        if (Settings.RunWithAnInMemoryDatabase)
        {
            services.UseInMemoryDatabase(CurrentTestInfo.Fetcher);

            // In-memory mode: CosmosClient is removed from DI, which would cause
            // the CosmosDb health check to report Unhealthy. Replace with a no-op.
            services.ReplaceCosmosDbHealthCheckWithNoOp();
        }
        else if (_staticServiceProvider != null)
        {
            // Docker mode: reuse the static factory's CosmosClient and Container so
            // delayed per-scenario factories don't open additional connections to the
            // emulator — the Cosmos Linux emulator is fragile under concurrent load.
            services.RemoveAll<Microsoft.Azure.Cosmos.CosmosClient>();
            services.AddSingleton(_staticServiceProvider.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>());
            services.RemoveAll<Microsoft.Azure.Cosmos.Container>();
            services.AddSingleton(_staticServiceProvider.GetRequiredService<Microsoft.Azure.Cosmos.Container>());

            // Docker mode: per-scenario factories must not spawn their own Kafka/EventHub
            // consumers — they join the same consumer group as the static factory's consumers
            // and compete for partition ownership on single-partition topics, causing
            // rebalance storms and message processing gaps. Only the static factory's
            // background consumers should be active; they write to the shared SQL Server
            // reporting DB that all factories' GraphQL endpoints read from.
            if (!Settings.RunWithAnInMemoryKafkaBroker)
            {
                var kafkaConsumer = services
                    .FirstOrDefault(d => d.ImplementationType == typeof(ReportingKafkaConsumerService));
                if (kafkaConsumer is not null)
                    services.Remove(kafkaConsumer);
            }

            if (!Settings.RunWithAnInMemoryEventHub)
            {
                var ehConsumer = services
                    .FirstOrDefault(d => d.ImplementationType == typeof(EventHubEquipmentAlertConsumerService));
                if (ehConsumer is not null)
                    services.Remove(ehConsumer);
            }
        }

        if (Settings.RunWithAnInMemoryReportingDatabase)
        {
            services.UseInMemoryReportingDatabase();

            // UseInMemoryReportingDatabase replaces the real Kafka consumer with
            // an in-memory variant that listens to ConsumedKafkaMessageStore events.
            // In Docker mode (real Kafka broker), the real producer doesn't fire
            // those events, so re-register the real consumer to read from Kafka
            // and write to the (now SQLite) reporting DB.
            if (!Settings.RunWithAnInMemoryKafkaBroker)
            {
                var inMemoryKafkaConsumer = services
                    .FirstOrDefault(d => d.ImplementationType == typeof(InMemoryReportingKafkaConsumerService));
                if (inMemoryKafkaConsumer is not null)
                    services.Remove(inMemoryKafkaConsumer);

                services.AddHostedService<ReportingKafkaConsumerService>();
            }
        }

        if (Settings.RunWithAnInMemoryBreakfastDatabase)
            services.UseInMemoryBreakfastDatabase();

        if (Settings.RunWithAnInMemorySpannerDatabase)
        {
            services.UseInMemorySpannerDatabase(_fakeSpannerServer!, CurrentTestInfo.Fetcher);
            services.ReplaceSpannerHealthCheckWithNoOp();
        }

        if (Settings.RunWithAnInMemoryEventGrid)
            services.UseInMemoryEventGrid();
        else
        {
            services.UseSelfSignedEventGridCertificate();

            // Docker mode: register a store backed by the Azurite storage queue
            // so that tests can read events the API published through the real
            // EventGrid simulator end-to-end.
            services.AddSingleton(_ => TestServiceCollectionExtensions.SharedQueueDrainer!);
            services.AddSingleton<Shared.Fakes.EventGrid.IPublishedEventStore>(
                _ => new Shared.Fakes.EventGrid.EventGridStorageQueueEventStore(
                    TestServiceCollectionExtensions.SharedQueueDrainer!,
                    "OrderCreatedEvent"));
        }

        // Register the shared consumed store so InMemoryProducer writes to it
        services.AddSingleton(_ => ConsumedKafkaMessageStore);
        if (Settings.RunWithAnInMemoryKafkaBroker)
            services.UseInMemoryKafkaBroker(ConsumedKafkaMessageStore);

        // In-memory mode: Kafka health check would fail because there's no real broker.
        // Replace with a no-op that reports Healthy.
        if (Settings.RunWithAnInMemoryKafkaBroker)
            services.ReplaceKafkaHealthCheckWithNoOp();

        // Docker mode: resolve relative SslCaLocation to absolute by walking up from CWD
        // (test CWD is bin/Debug/net10.0/, but the cert is at the repo root).
        if (!Settings.RunWithAnInMemoryKafkaBroker)
        {
            services.PostConfigure<KafkaConfig>(config =>
            {
                if (string.IsNullOrEmpty(config.SslCaLocation) || Path.IsPathRooted(config.SslCaLocation))
                    return;

                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (dir != null)
                {
                    var candidate = Path.Combine(dir.FullName, config.SslCaLocation);
                    if (File.Exists(candidate))
                    {
                        config.SslCaLocation = candidate;
                        return;
                    }
                    dir = dir.Parent;
                }
            });
        }

        // Google Cloud Pub/Sub — always in-memory for component tests
        services.AddSingleton(_ => ConsumedPubSubMessageStore);
        services.UseInMemoryPubSub(ConsumedPubSubMessageStore);
        services.ReplacePubSubHealthCheckWithNoOp();

        // UseInMemoryReportingDatabase already replaces PubSubBatchCompletionConsumerService
        // with InMemoryPubSubBatchCompletionConsumerService. When the reporting DB is NOT
        // in-memory, we still need this replacement because the real consumer requires
        // real GCP Pub/Sub infrastructure which isn't available in tests.
        if (!Settings.RunWithAnInMemoryReportingDatabase)
        {
            var realPubSubConsumer = services
                .FirstOrDefault(d => d.ImplementationType == typeof(PubSubBatchCompletionConsumerService));
            if (realPubSubConsumer is not null)
                services.Remove(realPubSubConsumer);

            services.AddHostedService<InMemoryPubSubBatchCompletionConsumerService>();
        }

        // Azure Event Hub — conditional based on settings
        services.AddSingleton(_ => ConsumedEventHubMessageStore);
        if (Settings.RunWithAnInMemoryEventHub)
            services.UseInMemoryEventHub(ConsumedEventHubMessageStore);
        else
            services.UseRealEventHub();

        // gRPC Notification Service — tracked client pointing at fake service
        if (Settings.RunWithAnInMemoryNotificationService)
            services.UseTrackedGrpcNotificationClient(CurrentTestInfo.Fetcher, Settings.NotificationServiceBaseUrl!);

        // Tracking wrappers must be registered AFTER the in-memory/real publishers
        services.UseTrackedOutboxWriter(CurrentTestInfo.Fetcher);
        services.UseTrackedKafkaProducer(CurrentTestInfo.Fetcher);
        services.UseTrackedPubSubPublishers();

        // Register Kafka message store (avoids src/ model imports in step files)
        services.AddTestTypedEventStores(ConsumedKafkaMessageStore, ConsumedEventHubMessageStore, CurrentTestInfo.Fetcher);
    }

    private HttpClient CreateTestClient()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
        {
            var handler = new TestTrackingMessageHandler(
                new LightBddTestTrackingMessageHandlerOptions
                {
                    FixedNameForReceivingService = Documentation.ServiceNames.BreakfastProvider
                })
            {
                InnerHandler = new HttpClientHandler()
            };

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(Settings.ExternalServiceUnderTestUrl!),
                Timeout = TimeSpan.FromMinutes(5)
            };
            return client;
        }

        var testClient = AppFactory.CreateDefaultClient(
            new TestTrackingMessageHandler(
                new LightBddTestTrackingMessageHandlerOptions
                {
                    FixedNameForReceivingService = Documentation.ServiceNames.BreakfastProvider
                }));

        // In Docker mode, set an explicit timeout so tests fail fast instead
        // of waiting the default 100 s when the Cosmos emulator is overloaded.
        if (!Settings.RunWithAnInMemoryDatabase)
            testClient.Timeout = TimeSpan.FromMinutes(3);

        return testClient;
    }

    protected T Get<T>() where T : notnull => _testServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        Client?.Dispose();
        if (_delayAppCreation && _appFactory != null && _appFactory != _staticFactory)
            _appFactory.Dispose();
    }

    public static void DisposeFactory() => _staticFactory?.Dispose();
}
