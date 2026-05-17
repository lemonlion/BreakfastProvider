using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using Kronikol.xUnit3;

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
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHealthCheckResponse>(content)!;
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthCheckStatuses.Healthy);
        result.Results.Should().ContainKey(HealthCheckNames.CowService);
        result.Results.Should().ContainKey(HealthCheckNames.GoatService);
        result.Results.Should().ContainKey(HealthCheckNames.SupplierService);
        result.Results.Should().ContainKey(HealthCheckNames.KitchenService);
        result.Results.Should().ContainKey(HealthCheckNames.CosmosDb);
        result.Results.Should().ContainKey(HealthCheckNames.Kafka);
    }
}
