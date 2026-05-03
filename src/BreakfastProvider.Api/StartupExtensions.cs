using Azure.Messaging.EventHubs.Producer;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Data;
using BreakfastProvider.Api.Data.Spanner;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Filters;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Reporting;
using BreakfastProvider.Api.Services;
using BreakfastProvider.Api.Validators;
using Bielu.AspNetCore.AsyncApi.Extensions;
using ByteBard.AsyncAPI;
using Google.Cloud.PubSub.V1;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api;

public static class StartupExtensions
{
    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new ProgramConfig { Name = nameof(BreakfastProvider), Namespace = "BreakfastProvider.Api" });
        services.AddSingleton<IProducerFactory, KafkaProducerFactory>();
        services.AddSingleton<IKafkaProducerConfigurationFactory, KafkaProducerConfigurationFactory>();
        services.AddSingleton<KafkaEventPublisher<RecipeLogEvent>>();
        services.AddOptions<ProgramSettings>().Bind(configuration);
        services.AddOptions<KafkaConfig>()
            .Bind(configuration.GetSection("KafkaConfig"))
            .Validate(configObject =>
            {
                var validator = new KafkaConfigValidator();
                var validationResult = validator.Validate(configObject);
                return validationResult.IsValid;
            })
            .ValidateOnStart();
        return services;
    }

    public static IServiceCollection AddPubSub(this IServiceCollection services, IConfiguration configuration)
    {
        var pubSubConfig = configuration.GetSection("PubSubConfig").Get<PubSubConfig>() ?? new PubSubConfig();
        services.AddOptions<PubSubConfig>()
            .Bind(configuration.GetSection("PubSubConfig"))
            .Validate(configObject =>
            {
                // When ProjectId is empty, Pub/Sub is disabled — skip validation.
                if (string.IsNullOrWhiteSpace(configObject.ProjectId))
                    return true;

                var validator = new PubSubConfigValidator();
                var validationResult = validator.Validate(configObject);
                return validationResult.IsValid;
            })
            .ValidateOnStart();

        if (string.IsNullOrWhiteSpace(pubSubConfig.ProjectId))
        {
            // Register typed PubSub publishers (no-op) so that DI resolves
            // even when there's no real Pub/Sub project configured.
            services.AddSingleton(_ => new PubSubEventPublisher<InventoryItemAddedEvent>());
            services.AddSingleton(_ => new PubSubEventPublisher<InventoryStockUpdatedEvent>());
            services.AddSingleton(_ => new PubSubEventPublisher<MenuAvailabilityChangedEvent>());
            services.AddSingleton(_ => new PubSubEventPublisher<ToppingCreatedEvent>());
            services.AddSingleton(_ => new PubSubEventPublisher<StaffMemberAddedEvent>());
            services.AddSingleton(_ => new PubSubEventPublisher<ReservationConfirmedEvent>());
            services.AddSingleton(_ => new PubSubEventPublisher<ReservationCancelledEvent>());
            services.AddSingleton(_ => new PubSubEventPublisher<DailySpecialOrderedEvent>());
            services.AddSingleton(_ => new PubSubEventPublisher<PancakeBatchCompletedEvent>());
            services.AddSingleton(_ => new PubSubEventPublisher<WaffleBatchCompletedEvent>());
            return services;
        }

        // Register a keyed PublisherClient per event type so each typed publisher
        // gets the client bound to the correct topic (not just the last registered one).
        foreach (var (eventTypeName, topicConfig) in pubSubConfig.PublisherConfigurations)
        {
            var topicName = TopicName.FromProjectTopic(pubSubConfig.ProjectId, topicConfig.TopicId);
            services.AddKeyedSingleton(eventTypeName, (sp, key) =>
                new PublisherClientBuilder
                {
                    TopicName = topicName,
                    EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOrProduction
                }.Build());
        }

        // Register typed PubSub publishers, each resolving the keyed PublisherClient for its event type
        RegisterKeyedPublisher<InventoryItemAddedEvent>(services);
        RegisterKeyedPublisher<InventoryStockUpdatedEvent>(services);
        RegisterKeyedPublisher<MenuAvailabilityChangedEvent>(services);
        RegisterKeyedPublisher<ToppingCreatedEvent>(services);
        RegisterKeyedPublisher<StaffMemberAddedEvent>(services);
        RegisterKeyedPublisher<ReservationConfirmedEvent>(services);
        RegisterKeyedPublisher<ReservationCancelledEvent>(services);
        RegisterKeyedPublisher<DailySpecialOrderedEvent>(services);
        RegisterKeyedPublisher<PancakeBatchCompletedEvent>(services);
        RegisterKeyedPublisher<WaffleBatchCompletedEvent>(services);

        return services;
    }

    private static void RegisterKeyedPublisher<T>(IServiceCollection services) where T : IPubSubEvent
    {
        services.AddSingleton(sp =>
        {
            var publisher = sp.GetRequiredKeyedService<PublisherClient>(typeof(T).Name);
            return new PubSubEventPublisher<T>(
                sp.GetRequiredService<IOptions<PubSubConfig>>(),
                publisher,
                sp.GetRequiredService<ILogger<PubSubEventPublisher<T>>>(),
                sp.GetRequiredService<IHttpContextAccessor>());
        });
    }

    public static IServiceCollection AddEventHub(this IServiceCollection services, IConfiguration configuration)
    {
        var eventHubConfig = configuration.GetSection(nameof(EventHubConfig)).Get<EventHubConfig>() ?? new EventHubConfig();
        services.AddOptions<EventHubConfig>()
            .Bind(configuration.GetSection(nameof(EventHubConfig)));

        if (string.IsNullOrWhiteSpace(eventHubConfig.ConnectionString))
        {
            // Register no-op publisher so DI resolves even without a real Event Hub.
            services.AddSingleton(_ => new EventHubEventPublisher<EquipmentAlertEvent>());
            return services;
        }

        services.AddSingleton(_ =>
            new EventHubProducerClient(eventHubConfig.ConnectionString, eventHubConfig.EventHubName));

        services.AddSingleton<EventHubEventPublisher<EquipmentAlertEvent>>();

        return services;
    }

    public static IServiceCollection AddAsyncApi(this IServiceCollection services)
    {
        services.AddAsyncApi("v1", options =>
        {
            options.AsyncApiVersion = AsyncApiVersion.AsyncApi3_0;
            options.WithInfo(Documentation.ServiceTitle, "1.0.0");
            options.WithDescription($"AsyncAPI for {Documentation.ServiceTitle}");
            options.WithDefaultContentType("application/json");
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.DefaultContentType ??= "application/json";
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer<AsyncApiDynamicConfigurationProviderFilter>();
        });

        return services;
    }
    public static IServiceCollection AddReporting(this IServiceCollection services, IConfiguration configuration)
    {
        var reportingConfig = configuration.GetSection(nameof(ReportingConfig)).Get<ReportingConfig>()
            ?? new ReportingConfig();
        services.Configure<ReportingConfig>(configuration.GetSection(nameof(ReportingConfig)));

        services.AddPooledDbContextFactory<ReportingDbContext>(options =>
        {
            options.UseSqlServer(reportingConfig.ConnectionString);
        });

        services.AddScoped<ReportingDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<ReportingDbContext>>().CreateDbContext());

        services.AddScoped<IReportingIngester, ReportingIngester>();
        services.AddHostedService<ReportingKafkaConsumerService>();
        services.AddHostedService<PubSubBatchCompletionConsumerService>();
        services.AddHostedService<EventHubEquipmentAlertConsumerService>();

        services.AddGraphQLServer()
            .AddQueryType<ReportingQuery>()
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .RegisterDbContextFactory<ReportingDbContext>();

        return services;
    }

    public static IServiceCollection AddBreakfastDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseConfig = configuration.GetSection(nameof(DatabaseConfig)).Get<DatabaseConfig>()
            ?? new DatabaseConfig();
        services.Configure<DatabaseConfig>(configuration.GetSection(nameof(DatabaseConfig)));

        services.AddPooledDbContextFactory<BreakfastDbContext>(options =>
        {
            options.UseSqlServer(databaseConfig.ConnectionString);
        });

        services.AddScoped<BreakfastDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<BreakfastDbContext>>().CreateDbContext());

        return services;
    }

    public static IServiceCollection AddSpannerDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var spannerConfig = configuration.GetSection(nameof(SpannerConfig)).Get<SpannerConfig>()
            ?? new SpannerConfig();
        services.AddOptions<SpannerConfig>()
            .Bind(configuration.GetSection(nameof(SpannerConfig)))
            .Validate(configObject =>
            {
                // When ProjectId is empty, Spanner is disabled — skip validation.
                if (string.IsNullOrWhiteSpace(configObject.ProjectId))
                    return true;

                var validator = new SpannerConfigValidator();
                return validator.Validate(configObject).IsValid;
            })
            .ValidateOnStart();

        services.AddSingleton<ISpannerConnectionFactory>(
            string.IsNullOrWhiteSpace(spannerConfig.ProjectId)
                ? new NoOpSpannerConnectionFactory()
                : new SpannerConnectionFactory(spannerConfig.ConnectionString));

        return services;
    }

    public static IServiceCollection AddNotificationGrpcClient(this IServiceCollection services, IConfiguration configuration)
    {
        var notificationConfig = configuration.GetSection(nameof(NotificationServiceConfig)).Get<NotificationServiceConfig>()
            ?? new NotificationServiceConfig();

        var address = string.IsNullOrWhiteSpace(notificationConfig.BaseAddress)
            ? "http://localhost" // Placeholder — gRPC calls will fail at runtime if actually invoked.
            : notificationConfig.BaseAddress;

        services.AddSingleton(sp =>
        {
            var channel = GrpcChannel.ForAddress(address);
            return new Grpc.NotificationGrpc.NotificationGrpcClient(channel);
        });

        services.AddScoped<INotificationClient, GrpcNotificationClient>();

        return services;
    }
}

public static class Documentation
{
    public const string ServiceTitle = "Breakfast Provider";
    public const string HeartbeatStatus = "ok";

    public static class ServiceNames
    {
        public const string BreakfastProvider = "Breakfast Provider";
        public const string CosmosDb = "CosmosDB";
        public const string EventGrid = "Event Grid";
        public const string KafkaBroker = "Kafka Broker";
        public const string GoogleCloudPubSub = "Google Cloud Pub/Sub";
        public const string AzureEventHub = "Azure Event Hub";
        public const string ReportingDatabase = "Reporting Database (SQL Server)";
        public const string BreakfastDatabase = "Breakfast Database (SQL Server)";
        public const string Spanner = "Spanner";
        public const string NotificationService = "Notification Service (gRPC)";
    }
}
