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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spanner.InMemoryEmulator;
using TestTrackingDiagrams.xUnit3;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.xUnit.Infrastructure;

public abstract class BaseFixture : DiagrammedComponentTest, IDisposable
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
            ? throw new InvalidOperationException("AppFactory is not available in post-deployment mode.")
            : _sharedFactory ?? _appFactory ?? _staticFactory!;
    protected string RequestId { get; } = Guid.NewGuid().ToString();
    public static ComponentTestSettings Settings { get; private set; } = null!;
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

    protected void CreateAppAndClientWithSharedFactory(Dictionary<string, string?> settingOverrides)
    {
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
            services.ReplaceCosmosDbHealthCheckWithNoOp();
        }
        else if (_staticServiceProvider != null)
        {
            services.RemoveAll<Microsoft.Azure.Cosmos.CosmosClient>();
            services.AddSingleton(_staticServiceProvider.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>());
            services.RemoveAll<Microsoft.Azure.Cosmos.Container>();
            services.AddSingleton(_staticServiceProvider.GetRequiredService<Microsoft.Azure.Cosmos.Container>());

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
                _ => new Shared.Fakes.EventGrid.EventGridStorageQueueEventStore(
                    TestServiceCollectionExtensions.SharedQueueDrainer!,
                    "OrderCreatedEvent"));
        }

        services.AddSingleton(_ => ConsumedKafkaMessageStore);
        if (Settings.RunWithAnInMemoryKafkaBroker)
            services.UseInMemoryKafkaBroker(ConsumedKafkaMessageStore);

        if (Settings.RunWithAnInMemoryKafkaBroker)
            services.ReplaceKafkaHealthCheckWithNoOp();

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

        services.AddSingleton(_ => ConsumedPubSubMessageStore);
        if (Settings.RunWithAnInMemoryPubSub)
        {
            services.UseInMemoryPubSub(ConsumedPubSubMessageStore);
            services.ReplacePubSubHealthCheckWithNoOp();
        }

        if (Settings.RunWithAnInMemoryPubSub && !Settings.RunWithAnInMemoryReportingDatabase)
        {
            var realPubSubConsumer = services
                .FirstOrDefault(d => d.ImplementationType == typeof(PubSubBatchCompletionConsumerService));
            if (realPubSubConsumer is not null)
                services.Remove(realPubSubConsumer);

            services.AddHostedService<InMemoryPubSubBatchCompletionConsumerService>();
        }

        services.AddSingleton(_ => ConsumedEventHubMessageStore);
        if (Settings.RunWithAnInMemoryEventHub)
            services.UseInMemoryEventHub(ConsumedEventHubMessageStore);
        else
            services.UseRealEventHub();

        if (Settings.RunWithAnInMemoryNotificationService)
            services.UseTrackedGrpcNotificationClient(CurrentTestInfo.Fetcher, Settings.NotificationServiceBaseUrl!);

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
                new XUnitTestTrackingMessageHandlerOptions
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
                new XUnitTestTrackingMessageHandlerOptions
                {
                    FixedNameForReceivingService = Documentation.ServiceNames.BreakfastProvider
                }));

        if (!Settings.RunWithAnInMemoryDatabase)
            testClient.Timeout = TimeSpan.FromMinutes(3);

        return testClient;
    }

    protected T Get<T>() where T : notnull => _testServiceProvider.GetRequiredService<T>();

    protected static string? SkipIfExternalSut(string reason = "Needs in-memory infrastructure")
        => Settings.RunAgainstExternalServiceUnderTest ? reason : null;

    protected static string? SkipUnlessInMemoryDatabase(string reason = "Needs isolated in-memory database")
        => Settings.RunWithAnInMemoryDatabase ? null : reason;

    protected static string? SkipIfExternalSutOrSharedDocker(string reason = "Needs isolated database")
        => Settings.RunAgainstExternalServiceUnderTest || Settings.UsesSharedDockerDatabase ? reason : null;

    public new void Dispose()
    {
        Client?.Dispose();
        if (_delayAppCreation && _appFactory != null && _appFactory != _staticFactory)
            _appFactory.Dispose();
        base.Dispose();
    }

    public static void DisposeFactory() => _staticFactory?.Dispose();
}
