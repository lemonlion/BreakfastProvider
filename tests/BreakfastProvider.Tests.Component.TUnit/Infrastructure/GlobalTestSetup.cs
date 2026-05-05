using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Fakes.PubSub;
using BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TestTrackingDiagrams;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Infrastructure;

public class GlobalTestSetup
{
    private static readonly DateTime StartRunTime = DateTime.UtcNow;

    private static WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.CowService.Program>? _cowServiceFake;
    private static WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.GoatService.Program>? _goatServiceFake;
    private static WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.SupplierService.Program>? _supplierServiceFake;
    private static WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.KitchenService.Program>? _kitchenServiceFake;
    private static WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.NotificationService.Program>? _notificationServiceFake;

    private static readonly Dictionary<string, BackgroundService> KafkaConsumers = new();
    private static readonly Dictionary<string, BackgroundService> PubSubConsumers = new();
    private static readonly Shared.Infrastructure.DockerComposeOrchestrator DockerOrchestrator = new();

    private static ComponentTestSettings Settings { get; } = new ConfigurationBuilder().GetComponentTestSettings();

    [Before(Assembly)]
    public static async Task SetUp()
    {
        if (!Settings.RunWithAnInMemoryDatabase)
            ThreadPool.SetMinThreads(100, 100);

        StartDockerCompose();
        StartHttpFakes();
        TryRun("kafka consumers", StartKafkaConsumers);
        TryRun("pubsub consumers", StartPubSubConsumers);
        TryRun("eventgrid queue drainer", InitEventGridQueueDrainer);
        TryRun("clear docker queues", ClearDockerQueues);
        await BaseFixture.EnsureHostInitialized();
    }

    private static void TryRun(string stepName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GlobalTestSetup] Warning: '{stepName}' threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    [After(Assembly)]
    public static async Task TearDown()
    {
        BaseFixture.DisposeFactory();

        TUnitReportGenerator.CreateStandardReportsWithDiagrams(
            DiagrammedTestRun.TestContexts,
            StartRunTime,
            DateTime.UtcNow,
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "Breakfast Provider Specifications",
                WriteCiSummary = true
            });

        await SourceControlSpecificationsFile();
        DisposeKafkaConsumers();
        DisposePubSubConsumers();
        DisposeHttpFakes();
        StopDockerCompose();
    }

    private static async Task SourceControlSpecificationsFile()
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

    private static void StartHttpFakes()
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
            _notificationServiceFake = InMemoryFakeHelper.CreateForGrpc<Dependencies.Fakes.NotificationService.Program>(Settings.NotificationServiceBaseUrl!);
        }
    }

    private static void DisposeHttpFakes()
    {
        _cowServiceFake?.Dispose();
        _goatServiceFake?.Dispose();
        _supplierServiceFake?.Dispose();
        _kitchenServiceFake?.Dispose();
        _notificationServiceFake?.Dispose();
    }

    private static void StartKafkaConsumers()
    {
        if (Settings.RunWithAnInMemoryKafkaBroker)
            return;

        try { DisposeKafkaConsumers(); } catch { /* ignore */ }

        foreach (var (eventTypeName, _) in Settings.KafkaConfig.ConsumerConfigurations)
        {
            KafkaConsumers.Add(eventTypeName,
                new RawJsonKafkaConsumer(Settings.KafkaConfig, eventTypeName, BaseFixture.ConsumedKafkaMessageStore));
        }

        foreach (var consumer in KafkaConsumers.Values)
            consumer.StartAsync(CancellationToken.None);
    }

    private static void DisposeKafkaConsumers()
    {
        if (KafkaConsumers.Count == 0)
            return;

        foreach (var (name, consumer) in KafkaConsumers)
        {
            try { consumer.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
            catch (Exception ex) { Console.WriteLine($"[KafkaConsumer] Warning: StopAsync for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }

            try { consumer.Dispose(); }
            catch (Exception ex) { Console.WriteLine($"[KafkaConsumer] Warning: Dispose for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }
        }

        KafkaConsumers.Clear();
    }

    private static void StartPubSubConsumers()
    {
        if (Settings.RunWithAnInMemoryPubSub)
            return;

        try { DisposePubSubConsumers(); } catch { /* ignore */ }

        Environment.SetEnvironmentVariable("PUBSUB_EMULATOR_HOST", "localhost:8085");

        foreach (var (eventTypeName, topicConfig) in Settings.PubSubConfig.PublisherConfigurations)
        {
            PubSubConsumers.Add(eventTypeName,
                new RawJsonPubSubConsumer(
                    Settings.PubSubConfig.ProjectId, topicConfig.TopicId, eventTypeName,
                    BaseFixture.ConsumedPubSubMessageStore));
        }

        foreach (var consumer in PubSubConsumers.Values)
            consumer.StartAsync(CancellationToken.None);
    }

    private static void DisposePubSubConsumers()
    {
        if (PubSubConsumers.Count == 0)
            return;

        foreach (var (name, consumer) in PubSubConsumers)
        {
            try { consumer.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
            catch (Exception ex) { Console.WriteLine($"[PubSubConsumer] Warning: StopAsync for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }

            try { consumer.Dispose(); }
            catch (Exception ex) { Console.WriteLine($"[PubSubConsumer] Warning: Dispose for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }
        }

        PubSubConsumers.Clear();
    }

    private static void InitEventGridQueueDrainer()
    {
        if (Settings.RunWithAnInMemoryEventGrid)
            return;

        TestServiceCollectionExtensions.InitQueueDrainer(Settings.ExternalBlobStorageConnectionString!);
    }

    private static void ClearDockerQueues()
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

    private static void StartDockerCompose() => DockerOrchestrator.Start(Settings);

    private static void StopDockerCompose() => DockerOrchestrator.Dispose();
}
