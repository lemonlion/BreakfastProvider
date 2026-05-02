using System.Collections.Concurrent;
using BreakfastProvider.Api;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Tests.Component.Shared.Fakes.EventHub;
using BreakfastProvider.Tests.Component.Shared.Fakes.HttpFakes;
using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spanner.InMemoryEmulator;
using TestTrackingDiagrams.ReqNRoll;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.ReqNRoll.Support;

/// <summary>
/// Manages the WebApplicationFactory lifecycle for ReqNRoll scenarios.
/// Scoped per-scenario via DI.
/// </summary>
public sealed class AppManager : IDisposable
{
    private static WebApplicationFactory<Program>? _staticFactory;
    private static IServiceProvider? _staticServiceProvider;
    private static readonly ConcurrentDictionary<string, Lazy<WebApplicationFactory<Program>>> SharedFactoryCache = new();

    private WebApplicationFactory<Program>? _scenarioFactory;
    private WebApplicationFactory<Program>? _sharedFactory;
    private HttpClient? _client;
    private bool _delayedCreation;

    public static ComponentTestSettings Settings { get; private set; } = null!;
    public static FakeRequestStore FakeRequestStore { get; } = new();
    public static readonly ConsumedKafkaMessageStore ConsumedKafkaMessageStore = new();
    public static readonly ConsumedPubSubMessageStore ConsumedPubSubMessageStore = new();
    public static readonly ConsumedEventHubMessageStore ConsumedEventHubMessageStore = new();
    private static FakeSpannerServer? _fakeSpannerServer;
    internal static HttpMessageHandler? NotificationServiceHandler;

    public string RequestId { get; } = Guid.NewGuid().ToString();

    public HttpClient Client
    {
        get => _client ?? throw new InvalidOperationException("Client has not been created. Call EnsureDefaultApp() or CreateAppWithOverrides() first.");
        private set => _client = value;
    }

    public WebApplicationFactory<Program> AppFactory =>
        Settings.RunAgainstExternalServiceUnderTest
            ? throw new InvalidOperationException("AppFactory is not available in post-deployment mode.")
            : _sharedFactory ?? _scenarioFactory ?? _staticFactory!;

    public static void InitializeSettings()
    {
        var configuration = new ConfigurationBuilder().GetComponentTestConfiguration();
        Settings = configuration.Get<ComponentTestSettings>()!;
    }

    public static async Task EnsureHostInitialized()
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

    public void EnsureDefaultApp()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
            EnsureStaticFactoryCreated();
        Client = CreateTestClient();
    }

    public void SetDelayedCreation()
    {
        _delayedCreation = true;
    }

    public void CreateAppWithOverrides(
        Dictionary<string, string?>? configOverrides = null,
        Action<IServiceCollection>? additionalServices = null)
    {
        if ((configOverrides != null && configOverrides.Count > 0) || additionalServices != null)
        {
            _scenarioFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory())
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

    public void CreateAppWithSharedFactory(Dictionary<string, string?> settingOverrides)
    {
        if (Settings.RunWithAnInMemoryDatabase)
        {
            CreateAppWithOverrides(settingOverrides);
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
                config.SetBasePath(Directory.GetCurrentDirectory())
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
            services.ReplaceCosmosDbHealthCheckWithNoOp();
        }
        else if (_staticServiceProvider != null)
        {
            services.RemoveAll<Microsoft.Azure.Cosmos.CosmosClient>();
            services.AddSingleton(_staticServiceProvider.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>());
            services.RemoveAll<Microsoft.Azure.Cosmos.Container>();
            services.AddSingleton(_staticServiceProvider.GetRequiredService<Microsoft.Azure.Cosmos.Container>());
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
            services.AddSingleton(_ => TestServiceCollectionExtensions.SharedQueueDrainer!);
            services.AddSingleton<Shared.Fakes.EventGrid.IPublishedEventStore>(
                sp => new Shared.Fakes.EventGrid.EventGridStorageQueueEventStore(
                    sp.GetRequiredService<Shared.Fakes.EventGrid.EventGridQueueDrainer>(),
                    "OrderCreatedEvent"));
        }

        services.AddSingleton(_ => ConsumedKafkaMessageStore);
        if (Settings.RunWithAnInMemoryKafkaBroker)
            services.UseInMemoryKafkaBroker(ConsumedKafkaMessageStore);

        if (Settings.RunWithAnInMemoryKafkaBroker)
            services.ReplaceKafkaHealthCheckWithNoOp();

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

        // Azure Event Hub — always in-memory for component tests
        services.AddSingleton(_ => ConsumedEventHubMessageStore);
        services.UseInMemoryEventHub(ConsumedEventHubMessageStore);

        // gRPC Notification Service — tracked client pointing at fake service
        if (Settings.RunWithAnInMemoryNotificationService)
            services.UseTrackedGrpcNotificationClient(CurrentTestInfo.Fetcher, NotificationServiceHandler!);

        services.UseTrackedOutboxWriter(CurrentTestInfo.Fetcher);
        services.UseTrackedKafkaProducer(CurrentTestInfo.Fetcher);
        services.UseTrackedPubSubPublishers();

        services.AddTestTypedEventStores(ConsumedKafkaMessageStore, ConsumedEventHubMessageStore, CurrentTestInfo.Fetcher);
    }

    private HttpClient CreateTestClient()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
        {
            var handler = new TestTrackingMessageHandler(
                new ReqNRollTestTrackingMessageHandlerOptions
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
                new ReqNRollTestTrackingMessageHandlerOptions
                {
                    FixedNameForReceivingService = Documentation.ServiceNames.BreakfastProvider
                }));

        if (!Settings.RunWithAnInMemoryDatabase)
            testClient.Timeout = TimeSpan.FromMinutes(3);

        return testClient;
    }

    public static void DisposeFactory() => _staticFactory?.Dispose();

    public void Dispose()
    {
        _client?.Dispose();
        if (_delayedCreation && _scenarioFactory != null && _scenarioFactory != _staticFactory)
            _scenarioFactory.Dispose();
    }
}
