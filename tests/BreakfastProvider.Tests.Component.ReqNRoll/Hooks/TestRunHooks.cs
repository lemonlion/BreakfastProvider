using BreakfastProvider.Tests.Component.Shared.Fakes.Kafka;
using BreakfastProvider.Tests.Component.Shared.Infrastructure;
using Microsoft.Extensions.Hosting;
using Reqnroll;
using TestTrackingDiagrams;
using TestTrackingDiagrams.ReqNRoll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.Hooks;

[Binding]
public sealed class TestRunHooks
{
    private static WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.CowService.Program>? _cowServiceFake;
    private static WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.GoatService.Program>? _goatServiceFake;
    private static WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.SupplierService.Program>? _supplierServiceFake;
    private static WebApplicationFactoryForSpecificUrl<Dependencies.Fakes.KitchenService.Program>? _kitchenServiceFake;
    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Dependencies.Fakes.NotificationService.Program>? _notificationServiceFake;
    private static readonly Dictionary<string, BackgroundService> KafkaConsumers = new();
    private static readonly DockerComposeOrchestrator DockerOrchestrator = new();

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        Support.AppManager.InitializeSettings();
        var settings = Support.AppManager.Settings;

        if (!settings.RunWithAnInMemoryDatabase)
            ThreadPool.SetMinThreads(100, 100);

        DockerOrchestrator.Start(settings);

        if (!settings.RunAgainstExternalServiceUnderTest)
        {
            StartHttpFakes(settings);
            StartKafkaConsumers(settings);
            InitEventGridQueueDrainer(settings);
            ClearDockerQueues(settings);
        }

        await Support.AppManager.EnsureHostInitialized();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        ReqNRollReportGenerator.CreateStandardReportsWithDiagrams(
            new ReportConfigurationOptions
            {
                SpecificationsTitle = "Breakfast Provider Specifications"
            });

        Support.AppManager.DisposeFactory();
        DisposeKafkaConsumers();
        DisposeHttpFakes();
        DockerOrchestrator.Dispose();
    }

    private static void StartHttpFakes(ComponentTestSettings settings)
    {
        if (settings.RunAgainstExternalServiceUnderTest)
            return;

        if (settings.RunWithAnInMemoryCowService)
            _cowServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.CowService.Program>(settings.CowServiceBaseUrl!);

        if (settings.RunWithAnInMemoryGoatService)
            _goatServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.GoatService.Program>(settings.GoatServiceBaseUrl!);

        if (settings.RunWithAnInMemorySupplierService)
            _supplierServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.SupplierService.Program>(settings.SupplierServiceBaseUrl!);

        if (settings.RunWithAnInMemoryKitchenService)
            _kitchenServiceFake = InMemoryFakeHelper.Create<Dependencies.Fakes.KitchenService.Program>(settings.KitchenServiceBaseUrl!);

        if (settings.RunWithAnInMemoryNotificationService)
        {
            _notificationServiceFake = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Dependencies.Fakes.NotificationService.Program>();
            Support.AppManager.NotificationServiceHandler = _notificationServiceFake.Server.CreateHandler();
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

    private static void StartKafkaConsumers(ComponentTestSettings settings)
    {
        if (settings.RunWithAnInMemoryKafkaBroker)
            return;

        foreach (var (eventTypeName, _) in settings.KafkaConfig.ConsumerConfigurations)
        {
            KafkaConsumers.Add(eventTypeName,
                new RawJsonKafkaConsumer(settings.KafkaConfig, eventTypeName, Support.AppManager.ConsumedKafkaMessageStore));
        }

        foreach (var consumer in KafkaConsumers.Values)
            consumer.StartAsync(CancellationToken.None);
    }

    private static void DisposeKafkaConsumers()
    {
        foreach (var (name, consumer) in KafkaConsumers)
        {
            try { consumer.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
            catch (Exception ex) { Console.WriteLine($"[KafkaConsumer] Warning: StopAsync for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }

            try { consumer.Dispose(); }
            catch (Exception ex) { Console.WriteLine($"[KafkaConsumer] Warning: Dispose for '{name}' threw {ex.GetType().Name}: {ex.Message}"); }
        }

        KafkaConsumers.Clear();
    }

    private static void InitEventGridQueueDrainer(ComponentTestSettings settings)
    {
        if (settings.RunWithAnInMemoryEventGrid)
            return;

        TestServiceCollectionExtensions.InitQueueDrainer(settings.ExternalBlobStorageConnectionString!);
    }

    private static void ClearDockerQueues(ComponentTestSettings settings)
    {
        if (settings.RunWithAnInMemoryEventGrid)
            return;

        var connectionString = settings.ExternalBlobStorageConnectionString;
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
            // Queue might not exist yet
        }
    }
}
