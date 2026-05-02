using Azure;
using Azure.Messaging.EventGrid;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Data;
using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.HttpClients;
using BreakfastProvider.Api.Services;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Api.Telemetry;
using BreakfastProvider.Api.Reporting;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Bielu.AspNetCore.AsyncApi.Extensions;
using Bielu.AspNetCore.AsyncApi.UI;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using BreakfastProvider.Api.Services.HealthChecks;
using BreakfastProvider.Api.Validators;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Serilog
        builder.Host.UseSerilog((context, config) =>
            config.ReadFrom.Configuration(context.Configuration));

        // OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(DiagnosticsConfig.ServiceName))
            .WithTracing(tracing => tracing
                .AddSource(DiagnosticsConfig.ServiceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddMeter(DiagnosticsConfig.ServiceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            logging.AddOtlpExporter();
        });

        // Configuration
        builder.Services.AddOptions<CosmosConfig>()
            .Bind(builder.Configuration.GetSection(nameof(CosmosConfig)))
            .Validate(configObject =>
            {
                var validator = new CosmosConfigValidator();
                return validator.Validate(configObject).IsValid;
            })
            .ValidateOnStart();
        builder.Services.AddOptions<EventGridConfig>()
            .Bind(builder.Configuration.GetSection(nameof(EventGridConfig)))
            .Validate(configObject =>
            {
                var validator = new EventGridConfigValidator();
                return validator.Validate(configObject).IsValid;
            })
            .ValidateOnStart();
        builder.Services.Configure<OutboxConfig>(builder.Configuration.GetSection(nameof(OutboxConfig)));
        builder.Services.Configure<ToppingRulesConfig>(builder.Configuration.GetSection(nameof(ToppingRulesConfig)));
        builder.Services.Configure<FeatureSwitchesConfig>(builder.Configuration.GetSection(nameof(FeatureSwitchesConfig)));
        builder.Services.Configure<DailySpecialsConfig>(builder.Configuration.GetSection(nameof(DailySpecialsConfig)));
        builder.Services.Configure<OrderConfig>(builder.Configuration.GetSection(nameof(OrderConfig)));
        builder.Services.Configure<RateLimitConfig>(builder.Configuration.GetSection(nameof(RateLimitConfig)));

        // Rate limiting
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("OrderCreation", context =>
            {
                var config = context.RequestServices.GetRequiredService<IOptions<RateLimitConfig>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter("OrderCreation",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = config.PermitLimit,
                        Window = TimeSpan.FromSeconds(config.WindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        // Controllers & OpenAPI
        builder.Services.AddRouting(options => options.LowercaseUrls = true);
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks()
            .AddDownstreamServiceChecks()
            .AddInfrastructureChecks()
            .ForwardToPrometheus();
        builder.Services.AddMemoryCache();

        // Downstream HTTP services
        builder.Services.AddDownstreamServices(builder.Configuration);

        // Prometheus HttpClient metrics
        builder.Services.UseHttpClientMetrics();

        // Cosmos DB
        builder.Services.AddSingleton<CosmosClient>(sp =>
        {
            var cosmosConfig = builder.Configuration.GetSection(nameof(CosmosConfig)).Get<CosmosConfig>()!;
            var options = new CosmosClientOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(cosmosConfig.RequestTimeoutSeconds),
                MaxRetryAttemptsOnRateLimitedRequests = cosmosConfig.MaxRetryAttempts,
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            // Gateway mode with relaxed SSL validation — used for local development
            // against the Cosmos DB emulator (which uses a self-signed certificate).
            if (cosmosConfig.UseGatewayMode)
            {
                if (!builder.Environment.IsDevelopment())
                    throw new InvalidOperationException(
                        "UseGatewayMode is enabled outside of Development. " +
                        "This disables SSL certificate validation and must not be used in production.");

                options.ConnectionMode = ConnectionMode.Gateway;
                options.LimitToEndpoint = true;
                options.HttpClientFactory = () =>
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                        AllowAutoRedirect = false
                    };
                    return new HttpClient(handler);
                };
            }

            return new CosmosClient(cosmosConfig.ConnectionString, options);
        });
        builder.Services.AddSingleton(sp =>
        {
            var cosmosConfig = builder.Configuration.GetSection(nameof(CosmosConfig)).Get<CosmosConfig>()!;
            var client = sp.GetRequiredService<CosmosClient>();
            return client.GetContainer(cosmosConfig.DatabaseName, "orders");
        });
        builder.Services.AddSingleton<ICosmosRepository<OrderDocument>>(sp =>
            new CosmosRepository<OrderDocument>(sp.GetRequiredService<Container>()));
        builder.Services.AddSingleton<ICosmosRepository<RecipeDocument>>(sp =>
            new CosmosRepository<RecipeDocument>(sp.GetRequiredService<Container>()));
        builder.Services.AddSingleton<ICosmosRepository<AuditLogDocument>>(sp =>
            new CosmosRepository<AuditLogDocument>(sp.GetRequiredService<Container>()));
        builder.Services.AddSingleton<ICosmosRepository<OutboxMessage>>(sp =>
            new CosmosRepository<OutboxMessage>(sp.GetRequiredService<Container>()));
        builder.Services.AddSingleton<ICosmosRepository<IdempotencyRecord>>(sp =>
            new CosmosRepository<IdempotencyRecord>(sp.GetRequiredService<Container>()));
        builder.Services.AddScoped<IIdempotencyStore, CosmosIdempotencyStore>();

        // EventGrid
        var eventGridConfig = builder.Configuration.GetSection(nameof(EventGridConfig)).Get<EventGridConfig>();
        if (eventGridConfig?.IsEnabled == true)
        {
            builder.Services.AddSingleton(sp =>
            {
                var clientOptions = new EventGridPublisherClientOptions();

                // In Development mode, the EventGrid simulator uses a self-signed
                // certificate. Bypass SSL validation to avoid UntrustedRoot errors.
                if (builder.Environment.IsDevelopment())
                {
                    clientOptions.Transport = new Azure.Core.Pipeline.HttpClientTransport(
                        new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback =
                                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        });
                }

                return new EventGridPublisherClient(
                    new Uri(eventGridConfig.Endpoint),
                    new AzureKeyCredential(eventGridConfig.TopicKey),
                    clientOptions);
            });
        }

        // Outbox — OrderService writes OrderDocument + OrderCreatedEvent atomically via transactional batch;
        // OutboxProcessor dispatches pending messages via EventGrid.
        builder.Services.AddSingleton<IOutboxWriter>(sp =>
            new OutboxWriter(sp.GetRequiredService<Container>()));
        builder.Services.AddSingleton<IOutboxDispatcher, EventGridOutboxDispatcher>();
        builder.Services.AddHostedService<OutboxProcessor>();

        // Kafka
        builder.Services.AddKafka(builder.Configuration);

        // Google Cloud Pub/Sub
        builder.Services.AddPubSub(builder.Configuration);

        // Azure Event Hub
        builder.Services.AddEventHub(builder.Configuration);

        // AsyncAPI
        builder.Services.AddAsyncApi();

        // Reporting (Business Intelligence)
        builder.Services.AddReporting(builder.Configuration);

        // Breakfast Database (Inventory, Staff, Reservations)
        builder.Services.AddBreakfastDatabase(builder.Configuration);

        // Google Cloud Spanner (Feedback, Customer Preferences)
        builder.Services.AddSpannerDatabase(builder.Configuration);

        // gRPC
        builder.Services.AddGrpc();
        builder.Services.AddNotificationGrpcClient(builder.Configuration);

        // Services
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<IRecipeLogger, RecipeLogger>();
        builder.Services.AddScoped<IInventoryService, InventoryService>();
        builder.Services.AddScoped<IStaffService, StaffService>();
        builder.Services.AddScoped<IReservationService, ReservationService>();
        builder.Services.AddScoped<IFeedbackService, FeedbackService>();
        builder.Services.AddScoped<ICustomerPreferenceService, CustomerPreferenceService>();
        builder.Services.AddScoped<IDailySpecialsService, DailySpecialsService>();
        builder.Services.AddScoped<IPancakeService, PancakeService>();
        builder.Services.AddScoped<IWaffleService, WaffleService>();
        builder.Services.AddScoped<IMenuService, MenuService>();
        builder.Services.AddScoped<IMilkSourcingService, MilkSourcingService>();
        builder.Services.AddScoped<IToppingService, ToppingService>();

        // Validation
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddFluentValidationAutoValidation();

        var app = builder.Build();

        // Ensure Cosmos DB database and container exist (creates if needed).
        // Skip when CosmosClient is not registered (e.g. in-memory component tests).
        // Retry on transient failures — the Cosmos emulator may still be warming up.
        var cosmosClient = app.Services.GetService<CosmosClient>();
        if (cosmosClient is not null)
        {
            var cosmosStartupConfig = builder.Configuration.GetSection(nameof(CosmosConfig)).Get<CosmosConfig>()!;
            using var cosmosInitCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            const int maxRetries = 10;
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
                        cosmosStartupConfig.DatabaseName, cancellationToken: cosmosInitCts.Token);
                    await dbResponse.Database.CreateContainerIfNotExistsAsync(
                        "orders", "/partitionKey", cancellationToken: cosmosInitCts.Token);
                    break;
                }
                catch (OperationCanceledException)
                {
                    Console.Error.WriteLine("[Startup] Cosmos DB initialization timed out — continuing without pre-created database.");
                    break;
                }
                catch (Exception ex) when (attempt < maxRetries && ex is CosmosException or HttpRequestException)
                {
                    try { await Task.Delay(TimeSpan.FromSeconds(3), cosmosInitCts.Token); }
                    catch (OperationCanceledException)
                    {
                        Console.Error.WriteLine("[Startup] Cosmos DB initialization timed out — continuing without pre-created database.");
                        break;
                    }
                }
            }
        }

        // Ensure Reporting (SQL) database schema is created.
        {
            using var reportingInitCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                using var scope = app.Services.CreateScope();
                var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ReportingDbContext>>();
                await using var reportingDb = await dbContextFactory.CreateDbContextAsync(reportingInitCts.Token);
                await reportingDb.Database.EnsureCreatedAsync(reportingInitCts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("[Startup] Reporting database initialization timed out — continuing without pre-created schema.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Startup] Reporting database initialization failed — continuing without pre-created schema. Error: {ex.Message}");
            }
        }

        // Ensure Breakfast database schema is created.
        {
            using var breakfastDbInitCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                using var scope = app.Services.CreateScope();
                var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BreakfastDbContext>>();
                await using var breakfastDb = await dbContextFactory.CreateDbContextAsync(breakfastDbInitCts.Token);
                await breakfastDb.Database.EnsureCreatedAsync(breakfastDbInitCts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("[Startup] Breakfast database initialization timed out — continuing without pre-created schema.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Startup] Breakfast database initialization failed — continuing without pre-created schema. Error: {ex.Message}");
            }
        }

        app.MapOpenApi();
        app.MapScalarApiReference();

        app.UseMiddleware<Filters.CorrelationIdMiddleware>();
        app.UseHttpMetrics();
        app.UseRateLimiter();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapGrpcService<Grpc.BreakfastGrpcService>()
            .Add(b =>
            {
                // The gRPC package registers a global catch-all route
                // (ANY /{service}/{method}) for unimplemented services that
                // interferes with REST controller routes like GET /inventory/{id}.
                // Constrain all gRPC endpoints to POST only, since gRPC exclusively uses POST.
                b.Metadata.Add(new HttpMethodMetadata(["POST"]));
            });
        app.MapGraphQL();
        app.MapMetrics();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        });
        app.MapAsyncApi();
        app.MapAsyncApiUi();

        await app.RunAsync();
    }
}
