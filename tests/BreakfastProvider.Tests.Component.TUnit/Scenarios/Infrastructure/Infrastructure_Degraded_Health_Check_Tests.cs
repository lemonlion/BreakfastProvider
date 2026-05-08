using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using Microsoft.Extensions.DependencyInjection;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Infrastructure;

public class Infrastructure_Degraded_Health_Check_Tests : BaseFixture
{
    public Infrastructure_Degraded_Health_Check_Tests() : base(delayAppCreation: true) { }

    [Test]
    public async Task Health_check_should_report_degraded_when_cow_service_is_unavailable()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given the cow service is configured to be unreachable
        CreateAppAndClient(additionalServices: services =>
        {
            services.ReplaceHealthCheckWithDegraded(HealthCheckNames.CowService,
                $"{HealthCheckNames.CowService} is unreachable (simulated for test).");

            if (!Settings.RunWithAnInMemoryDatabase)
                services.ReplaceCosmosDbHealthCheckWithNoOp();
            if (!Settings.RunWithAnInMemoryKafkaBroker)
                services.ReplaceKafkaHealthCheckWithNoOp();
        });

        // When the health check endpoint is called
        var response = await Client.GetAsync(Endpoints.Health);

        // Then the response should indicate a degraded status
        await response.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHealthCheckResponse>(content)!;
        await result.Should().NotBeNull();
        await result.Status.Should().BeEqualTo(HealthCheckStatuses.Degraded);

        // And the cow service dependency should report degraded
        await result.Results.Should().ContainKey(HealthCheckNames.CowService);
        await result.Results[HealthCheckNames.CowService].Status.Should().BeEqualTo(HealthCheckStatuses.Degraded);
    }

    [Test]
    public async Task Health_check_should_report_degraded_when_multiple_downstream_services_are_unavailable()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given the cow service and supplier service are configured to be unreachable
        CreateAppAndClient(additionalServices: services =>
        {
            services.ReplaceHealthCheckWithDegraded(HealthCheckNames.CowService,
                $"{HealthCheckNames.CowService} is unreachable (simulated for test).");
            services.ReplaceHealthCheckWithDegraded(HealthCheckNames.SupplierService,
                $"{HealthCheckNames.SupplierService} is unreachable (simulated for test).");

            if (!Settings.RunWithAnInMemoryDatabase)
                services.ReplaceCosmosDbHealthCheckWithNoOp();
            if (!Settings.RunWithAnInMemoryKafkaBroker)
                services.ReplaceKafkaHealthCheckWithNoOp();
        });

        // When the health check endpoint is called
        var response = await Client.GetAsync(Endpoints.Health);

        // Then the response should indicate a degraded status
        await response.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHealthCheckResponse>(content)!;
        await result.Should().NotBeNull();
        await result.Status.Should().BeEqualTo(HealthCheckStatuses.Degraded);

        // And both dependencies should report degraded
        await result.Results.Should().ContainKey(HealthCheckNames.CowService);
        await result.Results[HealthCheckNames.CowService].Status.Should().BeEqualTo(HealthCheckStatuses.Degraded);
        await result.Results.Should().ContainKey(HealthCheckNames.SupplierService);
        await result.Results[HealthCheckNames.SupplierService].Status.Should().BeEqualTo(HealthCheckStatuses.Degraded);
    }
}
