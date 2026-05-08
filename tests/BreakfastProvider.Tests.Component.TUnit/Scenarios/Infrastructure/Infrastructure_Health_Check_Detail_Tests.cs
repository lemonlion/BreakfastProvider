using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Infrastructure;

public class Infrastructure_Health_Check_Detail_Tests : BaseFixture
{
    [Test]
    public async Task Health_check_response_should_include_description_and_data_for_each_entry()
    {
        // When the health check endpoint is called
        var response = await Client.GetAsync(Endpoints.Health);

        // Then the response should contain detailed entries
        await response.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHealthCheckResponse>(content)!;
        await result.Should().NotBeNull();

        // Each entry should have a status
        foreach (var entry in result.Results)
        {
            var healthCheckEntryStatus = entry.Value.Status;
            await healthCheckEntryStatus.Should().NotBeNull();
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
            await result.Results.Should().ContainKey(checkName);
            var healthCheckDescription = result.Results[checkName].Description;
            await healthCheckDescription.Should().NotBeNull();
        }

        // Each entry should have a data object
        foreach (var entry in result.Results)
        {
            var healthCheckEntryData = entry.Value.Data;
            await healthCheckEntryData.Should().NotBeNull();
        }
    }
}
