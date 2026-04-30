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
                var validator = new PubSubConfigValidator();
                var validationResult = validator.Validate(configObject);
                return validationResult.IsValid;
            })
            .ValidateOnStart();

        if (string.IsNullOrWhiteSpace(pubSubConfig.ProjectId))
            return services;

        // Register a PublisherClient per event type
        foreach (var (eventTypeName, topicConfig) in pubSubConfig.PublisherConfigurations)
        {
            var topicName = TopicName.FromProjectTopic(pubSubConfig.ProjectId, topicConfig.TopicId);
            services.AddSingleton(sp =>
                PublisherClient.CreateAsync(topicName).GetAwaiter().GetResult());
        }

        // Register typed PubSub publishers for each event type
        services.AddSingleton<PubSubEventPublisher<InventoryItemAddedEvent>>();
        services.AddSingleton<PubSubEventPublisher<InventoryStockUpdatedEvent>>();
        services.AddSingleton<PubSubEventPublisher<MenuAvailabilityChangedEvent>>();
        services.AddSingleton<PubSubEventPublisher<ToppingCreatedEvent>>();
        services.AddSingleton<PubSubEventPublisher<StaffMemberAddedEvent>>();
        services.AddSingleton<PubSubEventPublisher<ReservationConfirmedEvent>>();
        services.AddSingleton<PubSubEventPublisher<ReservationCancelledEvent>>();
        services.AddSingleton<PubSubEventPublisher<DailySpecialOrderedEvent>>();
        services.AddSingleton<PubSubEventPublisher<PancakeBatchCompletedEvent>>();
        services.AddSingleton<PubSubEventPublisher<WaffleBatchCompletedEvent>>();

        return services;
    }

    public static IServiceCollection AddEventHub(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EventHubConfig>()
            .Bind(configuration.GetSection(nameof(EventHubConfig)));

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
                var validator = new SpannerConfigValidator();
                return validator.Validate(configObject).IsValid;
            })
            .ValidateOnStart();

        services.AddSingleton<ISpannerConnectionFactory>(
            new SpannerConnectionFactory(spannerConfig.ConnectionString));

        return services;
    }

    public static IServiceCollection AddNotificationGrpcClient(this IServiceCollection services, IConfiguration configuration)
    {
        var notificationConfig = configuration.GetSection(nameof(NotificationServiceConfig)).Get<NotificationServiceConfig>()
            ?? new NotificationServiceConfig();

        services.AddSingleton(sp =>
        {
            var channel = GrpcChannel.ForAddress(notificationConfig.BaseAddress);
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
        public const string PrimaryCallerOfTheBreakfastProvider = "Caller";
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
