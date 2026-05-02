using BreakfastProvider.Api.Configuration;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration;

public record ComponentTestSettings
{
    public bool RunWithAnInMemoryCowService { get; set; }
    public bool RunWithAnInMemoryGoatService { get; set; }
    public bool RunWithAnInMemorySupplierService { get; set; }
    public bool RunWithAnInMemoryKitchenService { get; set; }
    public bool RunWithAnInMemoryDatabase { get; set; }
    public bool RunWithAnInMemoryEventGrid { get; set; }
    public bool RunWithAnInMemoryKafkaBroker { get; set; }
    public bool RunWithAnInMemoryReportingDatabase { get; set; }
    public bool RunWithAnInMemoryBreakfastDatabase { get; set; }
    public bool RunWithAnInMemorySpannerDatabase { get; set; }
    public bool RunWithAnInMemoryNotificationService { get; set; }
    public bool RunWithAnInMemoryEventHub { get; set; }
    public string? CowServiceBaseUrl { get; set; }
    public string? GoatServiceBaseUrl { get; set; }
    public string? SupplierServiceBaseUrl { get; set; }
    public string? KitchenServiceBaseUrl { get; set; }
    public string? NotificationServiceBaseUrl { get; set; }
    public string? PlantUmlServerBaseUrl { get; set; }
    public string? ExternalBlobStorageConnectionString { get; set; }
    public KafkaConfig KafkaConfig { get; set; } = new();
    public bool RunAgainstExternalServiceUnderTest { get; set; }
    public string? ExternalServiceUnderTestUrl { get; set; }
    public bool EnableDockerInSetupAndTearDown { get; set; }
    public bool SkipDockerTearDown { get; set; }

    /// <summary>
    /// True when tests target a shared Docker database (not in-memory, not external SUT).
    /// Tests that require an isolated database should be skipped in this mode.
    /// </summary>
    public bool UsesSharedDockerDatabase => !RunWithAnInMemoryDatabase && !RunAgainstExternalServiceUnderTest;
}
