using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Infrastructure;

[Binding]
public class HealthCheckSteps(AppManager appManager)
{
    private HttpResponseMessage? _healthResponse;
    private TestHealthCheckResponse? _healthCheckResult;

    [When("the health check endpoint is called")]
    public async Task WhenTheHealthCheckEndpointIsCalled()
    {
        _healthResponse = await appManager.Client.GetAsync(Endpoints.Health);
    }

    [Then("the health check response should indicate healthy with all dependencies")]
    public async Task ThenTheHealthCheckResponseShouldIndicateHealthyWithAllDependencies()
    {
        Track.That(() => _healthResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        var content = await _healthResponse!.Content.ReadAsStringAsync();
        _healthCheckResult = Json.Deserialize<TestHealthCheckResponse>(content)!;
        Track.That(() => _healthCheckResult.Should().NotBeNull());
        Track.That(() => _healthCheckResult!.Status.Should().Be(HealthCheckStatuses.Healthy));
        Track.That(() => _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.CowService));
        Track.That(() => _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.GoatService));
        Track.That(() => _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.SupplierService));
        Track.That(() => _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.KitchenService));
        Track.That(() => _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.CosmosDb));
        Track.That(() => _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.Kafka));
    }

    [Then("the health check response should contain detailed entries")]
    public async Task ThenTheHealthCheckResponseShouldContainDetailedEntries()
    {
        Track.That(() => _healthResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        var content = await _healthResponse!.Content.ReadAsStringAsync();
        _healthCheckResult = Json.Deserialize<TestHealthCheckResponse>(content)!;

        foreach (var entry in _healthCheckResult!.Results)
        {
            var healthCheckEntryStatus = entry.Value.Status;
            Track.That(() => healthCheckEntryStatus.Should().NotBeNullOrEmpty($"'{entry.Key}' should have a status"));
        }

        string[] downstreamChecks = [HealthCheckNames.CowService, HealthCheckNames.GoatService, HealthCheckNames.SupplierService, HealthCheckNames.KitchenService];
        foreach (var checkName in downstreamChecks)
        {
            Track.That(() => _healthCheckResult.Results.Should().ContainKey(checkName));
            var healthCheckDescription = _healthCheckResult.Results[checkName].Description;
            Track.That(() => healthCheckDescription.Should().NotBeNullOrEmpty($"'{checkName}' should have a description"));
        }

        foreach (var entry in _healthCheckResult.Results)
        {
            var healthCheckEntryData = entry.Value.Data;
            Track.That(() => healthCheckEntryData.Should().NotBeNull($"'{entry.Key}' should have a data object"));
        }
    }

    [Then("the health check response should indicate a degraded status")]
    public async Task ThenTheHealthCheckResponseShouldIndicateADegradedStatus()
    {
        Track.That(() => _healthResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        var content = await _healthResponse!.Content.ReadAsStringAsync();
        _healthCheckResult = Json.Deserialize<TestHealthCheckResponse>(content)!;
        Track.That(() => _healthCheckResult!.Status.Should().Be(HealthCheckStatuses.Degraded));
    }

    [Then("the cow service dependency should report degraded")]
    public void ThenTheCowServiceDependencyShouldReportDegraded()
    {
        Track.That(() => _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.CowService));
        var cowServiceHealthStatus = _healthCheckResult.Results[HealthCheckNames.CowService].Status;
        Track.That(() => cowServiceHealthStatus.Should().Be(HealthCheckStatuses.Degraded));
    }

    [Then("the supplier service dependency should report degraded")]
    public void ThenTheSupplierServiceDependencyShouldReportDegraded()
    {
        Track.That(() => _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.SupplierService));
        var supplierServiceHealthStatus = _healthCheckResult.Results[HealthCheckNames.SupplierService].Status;
        Track.That(() => supplierServiceHealthStatus.Should().Be(HealthCheckStatuses.Degraded));
    }

    [Then("the kitchen service dependency should report degraded with a status code description")]
    public void ThenTheKitchenServiceDependencyShouldReportDegradedWithAStatusCodeDescription()
    {
        Track.That(() => _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.KitchenService));
        var kitchenServiceHealthCheckEntry = _healthCheckResult.Results[HealthCheckNames.KitchenService];
        var kitchenServiceHealthStatus = kitchenServiceHealthCheckEntry.Status;
        Track.That(() => kitchenServiceHealthStatus.Should().Be(HealthCheckStatuses.Degraded));
        var kitchenServiceHealthDescription = kitchenServiceHealthCheckEntry.Description;
        Track.That(() => kitchenServiceHealthDescription.Should().Contain("503"));
    }
}
