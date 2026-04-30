using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using LightBDD.Core.Configuration;
using LightBDD.Framework.Configuration;
using LightBDD.Framework.Notification;
using LightBDD.XUnit3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using LightBDD.Core.ExecutionContext;
using TestTrackingDiagrams;
using TestTrackingDiagrams.LightBDD.xUnit3;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(BreakfastProvider.Tests.Component.LightBDD.Infrastructure.ConfiguredLightBddScopeAttribute))]

namespace BreakfastProvider.Tests.Component.LightBDD.Infrastructure;

public class ConfiguredLightBddScopeAttribute : LightBddScope
{
    private const string SpecificationsFileName = "ComponentSpecifications";

    private WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.CowService.Program>? _cowServiceFake;
    private WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.GoatService.Program>? _goatServiceFake;
    private WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.SupplierService.Program>? _supplierServiceFake;
    private WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.KitchenService.Program>? _kitchenServiceFake;
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Dependencies.Fakes.NotificationService.Program>? _notificationServiceFake;

    private readonly Dictionary<string, BackgroundService> _kafkaConsumers = new();
    private readonly DockerComposeOrchestrator _dockerOrchestrator = new();

    protected override void OnConfigure(LightBddConfiguration configuration)
    {
        // Docker mode: increase the minimum thread-pool size to avoid thread starvation.
        // The Cosmos SDK queues requests when no threads are available, creating a thundering
        // herd that overwhelms the emulator when threads finally become available. The .NET
        // default (ProcessorCount) is 2 on CI runners, which is far too low.
        // See: https://aka.ms/cosmosdb-tsg-request-timeout ("Ensure min threads are set")
        // In-memory mode this is unnecessary and can cause HotChocolate schema-build race
        // conditions under extreme parallelism.
        if (!Settings.RunWithAnInMemoryDatabase)
            ThreadPool.SetMinThreads(100, 100);

        var testAssembly = Assembly.GetAssembly(typeof(ConfiguredLightBddScopeAttribute))!;
        configuration.ReportWritersConfiguration().CreateStandardReportsWithDiagrams(
                new ReportConfigurationOptions
                {
                    SpecificationsTitle = "Breakfast Provider Specifications"
                });

        // To stop the output repeating the step name each step
        configuration.ProgressNotifierConfiguration().Clear().Append(
            new SimpleIndentedProgressNotifier(x
                => ScenarioExecutionContext.GetCurrentScenarioFixtureIfPresent<ITestOutputProvider>()?.TestOutput.WriteLine(x)));

        configuration.ExecutionExtensionsConfiguration()
            .RegisterGlobalTearDown("dispose factory", BaseFixture.DisposeFactory)
            .RegisterGlobalTearDown("process specifications file", SourceControlSpecificationsFile)
            .RegisterGlobalSetUp("docker compose", StartDockerCompose, StopDockerCompose)
            .RegisterGlobalSetUp("http fakes", StartHttpFakes, DisposeHttpFakes)
            .RegisterGlobalSetUp("kafka consumer", StartKafkaConsumers, DisposeKafkaConsumers)
            .RegisterGlobalSetUp("eventgrid queue drainer", InitEventGridQueueDrainer)
            .RegisterGlobalSetUp("clear docker queues", ClearDockerQueues)
            .RegisterGlobalSetUp("host init", BaseFixture.EnsureHostInitialized);
    }

    private async Task SourceControlSpecificationsFile()
    {
        var specsPath = $"Reports/{SpecificationsFileName}.yml";
        if (!File.Exists(specsPath)) return;

        var specs = await File.ReadAllTextAsync(specsPath);
        if (specs.Length is not 0)
        {
            specs = specs.Replace("\r\n", "\n");
            await File.WriteAllTextAsync($"../../../../../docs/{SpecificationsFileName}.yml", specs);
        }
    }

    private void StartHttpFakes()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        try { DisposeHttpFakes(); } catch { /* ignore */ }

        if (Settings.RunWithAnInMemoryCowService)
            _cowServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.CowService.Program>(Settings.CowServiceBaseUrl!);

        if (Settings.RunWithAnInMemoryGoatService)
            _goatServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.GoatService.Program>(Settings.GoatServiceBaseUrl!);

        if (Settings.RunWithAnInMemorySupplierService)
            _supplierServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.SupplierService.Program>(Settings.SupplierServiceBaseUrl!);

        if (Settings.RunWithAnInMemoryKitchenService)
            _kitchenServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.KitchenService.Program>(Settings.KitchenServiceBaseUrl!);

        if (Settings.RunWithAnInMemoryNotificationService)
        {
            _notificationServiceFake = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Dependencies.Fakes.NotificationService.Program>();
            BaseFixture.NotificationServiceHandler = _notificationServiceFake.Server.CreateHandler();
        }
    }

    private void DisposeHttpFakes()
    {
        _cowServiceFake?.Dispose();
        _goatServiceFake?.Dispose();
        _supplierServiceFake?.Dispose();
        _kitchenServiceFake?.Dispose();
        _notificationServiceFake?.Dispose();
    }

    private void StartKafkaConsumers()
    {
        if (Settings.RunWithAnInMemoryKafkaBroker)
            return;

        try { DisposeKafkaConsumers(); } catch { /* ignore */ }

        foreach (var (eventTypeName, _) in Settings.KafkaConfig.ConsumerConfigurations)
        {
            _kafkaConsumers.Add(eventTypeName,
                new RawJsonKafkaConsumer(Settings.KafkaConfig, eventTypeName, BaseFixture.ConsumedKafkaMessageStore));
        }

        foreach (var consumer in _kafkaConsumers.Values)
            consumer.StartAsync(CancellationToken.None);
    }

    private void DisposeKafkaConsumers()
    {
        if (_kafkaConsumers.Count == 0)
            return;

        foreach (var (name, consumer) in _kafkaConsumers)
        {
            try
            {
                consumer.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KafkaConsumer] Warning: StopAsync for '{name}' threw {ex.GetType().Name}: {ex.Message}");
            }

            try
            {
                consumer.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KafkaConsumer] Warning: Dispose for '{name}' threw {ex.GetType().Name}: {ex.Message}");
            }
        }

        _kafkaConsumers.Clear();
    }

    private void InitEventGridQueueDrainer()
    {
        if (Settings.RunWithAnInMemoryEventGrid)
            return;

        TestServiceCollectionExtensions.InitQueueDrainer(Settings.ExternalBlobStorageConnectionString!);
    }

    /// <summary>
    /// Clears all queue messages from previous Docker test runs so that
    /// the EventGrid queue drainer only sees events from the current run.
    /// Only runs in Docker mode (not in-memory).
    /// </summary>
    private void ClearDockerQueues()
    {
        if (Settings.RunWithAnInMemoryEventGrid)
            return;

        var connectionString = Settings.ExternalBlobStorageConnectionString;
        if (string.IsNullOrEmpty(connectionString))
            return;

        var queueClient = new Azure.Storage.Queues.QueueServiceClient(
            connectionString,
            new Azure.Storage.Queues.QueueClientOptions
            {
                MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64
            });

        try
        {
            var client = queueClient.GetQueueClient("eventgrid-events");
            client.ClearMessages();
        }
        catch
        {
            // Queue might not exist yet — ignore
        }
    }

    private ComponentTestSettings Settings { get; } = new ConfigurationBuilder().GetComponentTestSettings();

    private void StartDockerCompose() => _dockerOrchestrator.Start(Settings);

    private void StopDockerCompose() => _dockerOrchestrator.Dispose();
}
