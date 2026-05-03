using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Infrastructure;

public class Infrastructure_Health_Check_Tests : BaseFixture
{
    [Fact]
    [HappyPath]
    public async Task Health_check_endpoint_should_return_healthy_status_with_all_dependency_details()
    {
        // When the health check endpoint is called
        var response = await Client.GetAsync(Endpoints.Health);

        // Then the response should indicate healthy with all dependencies
        Track.That(() => response.StatusCode.Should().Be(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHealthCheckResponse>(content)!;
        Track.That(() => result.Should().NotBeNull());
        Track.That(() => result.Status.Should().Be(HealthCheckStatuses.Healthy));
        Track.That(() => result.Results.Should().ContainKey(HealthCheckNames.CowService));
        Track.That(() => result.Results.Should().ContainKey(HealthCheckNames.GoatService));
        Track.That(() => result.Results.Should().ContainKey(HealthCheckNames.SupplierService));
        Track.That(() => result.Results.Should().ContainKey(HealthCheckNames.KitchenService));
        Track.That(() => result.Results.Should().ContainKey(HealthCheckNames.CosmosDb));
        Track.That(() => result.Results.Should().ContainKey(HealthCheckNames.Kafka));
    }
}
