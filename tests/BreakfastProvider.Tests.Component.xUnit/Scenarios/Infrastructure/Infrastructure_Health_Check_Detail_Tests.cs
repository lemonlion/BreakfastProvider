using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Infrastructure;

public class Infrastructure_Health_Check_Detail_Tests : BaseFixture
{
    [Fact]
    public async Task Health_check_response_should_include_description_and_data_for_each_entry()
    {
        // When the health check endpoint is called
        var response = await Client.GetAsync(Endpoints.Health);

        // Then the response should contain detailed entries
        Track.That(() => response.StatusCode.Should().Be(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHealthCheckResponse>(content)!;
        Track.That(() => result.Should().NotBeNull());

        // Each entry should have a status
        foreach (var entry in result.Results)
        {
            var healthCheckEntryStatus = entry.Value.Status;
            Track.That(() => healthCheckEntryStatus.Should().NotBeNullOrEmpty(
                $"health check entry '{entry.Key}' should have a status"));
        }

        // Each downstream entry should have a description
        string[] downstreamChecks =
        [
            HealthCheckNames.CowService,
            HealthCheckNames.GoatService,
            HealthCheckNames.SupplierService,
            HealthCheckNames.KitchenService
        ];

        foreach (var checkName in downstreamChecks)
        {
            Track.That(() => result.Results.Should().ContainKey(checkName));
            var healthCheckDescription = result.Results[checkName].Description;
            Track.That(() => healthCheckDescription.Should().NotBeNullOrEmpty(
                $"health check entry '{checkName}' should have a description"));
        }

        // Each entry should have a data object
        foreach (var entry in result.Results)
        {
            var healthCheckEntryData = entry.Value.Data;
            Track.That(() => healthCheckEntryData.Should().NotBeNull(
                $"health check entry '{entry.Key}' should have a data object"));
        }
    }
}
