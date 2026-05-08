using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Infrastructure;

public class Infrastructure_Health_Check_Tests : BaseFixture
{
    [Test]
    [HappyPath]
    public async Task Health_check_endpoint_should_return_healthy_status_with_all_dependency_details()
    {
        // When the health check endpoint is called
        var response = await Client.GetAsync(Endpoints.Health);

        // Then the response should indicate healthy with all dependencies
        await response.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHealthCheckResponse>(content)!;
        await result.Should().NotBeNull();
        await result.Status.Should().BeEqualTo(HealthCheckStatuses.Healthy);
        await result.Results.Should().ContainKey(HealthCheckNames.CowService);
        await result.Results.Should().ContainKey(HealthCheckNames.GoatService);
        await result.Results.Should().ContainKey(HealthCheckNames.SupplierService);
        await result.Results.Should().ContainKey(HealthCheckNames.KitchenService);
        await result.Results.Should().ContainKey(HealthCheckNames.CosmosDb);
        await result.Results.Should().ContainKey(HealthCheckNames.Kafka);
    }
}
