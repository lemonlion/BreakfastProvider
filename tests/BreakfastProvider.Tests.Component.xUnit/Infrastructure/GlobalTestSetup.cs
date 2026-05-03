using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;
using BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TestTrackingDiagrams;
using TestTrackingDiagrams.xUnit3;

[assembly: Xunit.AssemblyFixture(typeof(BreakfastProvider.Tests.Component.xUnit.Infrastructure.GlobalTestSetup))]

namespace BreakfastProvider.Tests.Component.xUnit.Infrastructure;

public class GlobalTestSetup : IAsyncLifetime
{
    private readonly DateTime _startRunTime = DateTime.UtcNow;
    private readonly DiagrammedTestRun _diagrammedTestRun = new();

    private WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.CowService.Program>? _cowServiceFake;
    private WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.GoatService.Program>? _goatServiceFake;
    private WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.SupplierService.Program>? _supplierServiceFake;
    private WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.KitchenService.Program>? _kitchenServiceFake;
    private WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.NotificationService.Program>? _notificationServiceFake;

    private readonly Dictionary<string, BackgroundService> _kafkaConsumers = new();
    private readonly Dictionary<string, BackgroundService> _pubSubConsumers = new();
    private readonly Shared.Infrastructure.DockerComposeOrchestrator _dockerOrchestrator = new();

    private ComponentTestSettings Settings { get; } = new ConfigurationBuilder().GetComponentTestSettings();

    public async ValueTask InitializeAsync()
    {
        if (!Settings.RunWithAnInMemoryDatabase)
            ThreadPool.SetMinThreads(100, 100);

        StartDockerCompose();
        StartHttpFakes();
        StartKafkaConsumers();
        StartPubSubConsumers();
        InitEventGridQueueDrainer();
        ClearDockerQueues();
        await BaseFixture.EnsureHostInitialized();
    }

    public async ValueTask DisposeAsync()
    {
        BaseFixture.DisposeFactory();

        XUnitReportGenerator.CreateStandardReportsWithDiagrams(
            _diagrammedTestRun.TestContexts,
            _startRunTime,
            DateTime.UtcNow,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "Breakfast Provider Specifications"
            });

        await SourceControlSpecificationsFile();
        DisposeKafkaConsumers();
        DisposePubSubConsumers();
        DisposeHttpFakes();
        StopDockerCompose();
    }

    private async Task SourceControlSpecificationsFile()
    {
        var specsPath = "Reports/Specifications.yml";
        if (!File.Exists(specsPath)) return;

        var specs = await File.ReadAllTextAsync(specsPath);
        if (specs.Length is not 0)
        {
            specs = specs.Replace("\r\n", "\n");
            await File.WriteAllTextAsync("../../../../../docs/Specifications.yml", specs);
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
            _notificationServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.NotificationService.Program>(Settings.NotificationServiceBaseUrl!);
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
            try { consumer.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
            catch (Exception ex) { Console.WriteLine($"[KafkaConsumer] Warning: StopAsync for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }

            try { consumer.Dispose(); }
            catch (Exception ex) { Console.WriteLine($"[KafkaConsumer] Warning: Dispose for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }
        }

        _kafkaConsumers.Clear();
    }

    private void StartPubSubConsumers()
    {
        if (Settings.RunWithAnInMemoryPubSub)
            return;

        try { DisposePubSubConsumers(); } catch { /* ignore */ }

        Environment.SetEnvironmentVariable("PUBSUB_EMULATOR_HOST", "localhost:8085");

        foreach (var (eventTypeName, topicConfig) in Settings.PubSubConfig.PublisherConfigurations)
        {
            _pubSubConsumers.Add(eventTypeName,
                new RawJsonPubSubConsumer(
                    Settings.PubSubConfig.ProjectId, topicConfig.TopicId, eventTypeName,
                    BaseFixture.ConsumedPubSubMessageStore));
        }

        foreach (var consumer in _pubSubConsumers.Values)
            consumer.StartAsync(CancellationToken.None);
    }

    private void DisposePubSubConsumers()
    {
        if (_pubSubConsumers.Count == 0)
            return;

        foreach (var (name, consumer) in _pubSubConsumers)
        {
            try { consumer.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
            catch (Exception ex) { Console.WriteLine($"[PubSubConsumer] Warning: StopAsync for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }

            try { consumer.Dispose(); }
            catch (Exception ex) { Console.WriteLine($"[PubSubConsumer] Warning: Dispose for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }
        }

        _pubSubConsumers.Clear();
    }

    private void InitEventGridQueueDrainer()
    {
        if (Settings.RunWithAnInMemoryEventGrid)
            return;

        TestServiceCollectionExtensions.InitQueueDrainer(Settings.ExternalBlobStorageConnectionString!);
    }

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

    private void StartDockerCompose() => _dockerOrchestrator.Start(Settings);

    private void StopDockerCompose() => _dockerOrchestrator.Dispose();
}
