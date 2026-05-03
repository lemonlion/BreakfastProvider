using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using Microsoft.Extensions.DependencyInjection;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Infrastructure;

public class Infrastructure_Downstream_Error_Health_Check_Tests : BaseFixture
{
    public Infrastructure_Downstream_Error_Health_Check_Tests() : base(delayAppCreation: true) { }

    [Fact]
    public async Task Health_check_should_report_degraded_when_downstream_service_returns_non_success_status()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given the kitchen service health check is configured to use a failing endpoint
        CreateAppAndClient(additionalServices: services =>
        {
            services.ReplaceHealthCheckWithFailingEndpoint(HealthCheckNames.KitchenService, "health-degraded");

            if (!Settings.RunWithAnInMemoryDatabase)
                services.ReplaceCosmosDbHealthCheckWithNoOp();
        });

        // When the health check endpoint is called
        var response = await Client.GetAsync(Endpoints.Health);

        // Then the response should indicate a degraded status
        Track.That(() => response.StatusCode.Should().Be(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHealthCheckResponse>(content)!;
        Track.That(() => result.Should().NotBeNull());
        Track.That(() => result.Status.Should().Be(HealthCheckStatuses.Degraded));

        // And the kitchen service dependency should report degraded with a status code description
        Track.That(() => result.Results.Should().ContainKey(HealthCheckNames.KitchenService));

        var kitchenEntry = result.Results[HealthCheckNames.KitchenService];
        var kitchenServiceHealthStatus = kitchenEntry.Status;
        Track.That(() => kitchenServiceHealthStatus.Should().Be(HealthCheckStatuses.Degraded));
        var kitchenServiceHealthDescription = kitchenEntry.Description;
        Track.That(() => kitchenServiceHealthDescription.Should().Contain("503"));
    }
}
