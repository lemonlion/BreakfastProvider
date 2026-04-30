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
        _healthResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await _healthResponse!.Content.ReadAsStringAsync();
        _healthCheckResult = Json.Deserialize<TestHealthCheckResponse>(content)!;
        _healthCheckResult.Should().NotBeNull();
        _healthCheckResult!.Status.Should().Be(HealthCheckStatuses.Healthy);
        _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.CowService);
        _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.GoatService);
        _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.SupplierService);
        _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.KitchenService);
        _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.CosmosDb);
        _healthCheckResult.Results.Should().ContainKey(HealthCheckNames.Kafka);
    }

    [Then("the health check response should contain detailed entries")]
    public async Task ThenTheHealthCheckResponseShouldContainDetailedEntries()
    {
        _healthResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await _healthResponse!.Content.ReadAsStringAsync();
        _healthCheckResult = Json.Deserialize<TestHealthCheckResponse>(content)!;

        foreach (var entry in _healthCheckResult!.Results)
            entry.Value.Status.Should().NotBeNullOrEmpty($"'{entry.Key}' should have a status");

        string[] downstreamChecks = [HealthCheckNames.CowService, HealthCheckNames.GoatService, HealthCheckNames.SupplierService, HealthCheckNames.KitchenService];
        foreach (var checkName in downstreamChecks)
        {
            _healthCheckResult.Results.Should().ContainKey(checkName);
            _healthCheckResult.Results[checkName].Description.Should().NotBeNullOrEmpty($"'{checkName}' should have a description");
        }

        foreach (var entry in _healthCheckResult.Results)
            entry.Value.Data.Should().NotBeNull($"'{entry.Key}' should have a data object");
    }

    [Then("the health check response should indicate a degraded status")]
    public async Task ThenTheHealthCheckResponseShouldIndicateADegradedStatus()
    {
        _healthResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await _healthResponse!.Content.ReadAsStringAsync();
        _healthCheckResult = Json.Deserialize<TestHealthCheckResponse>(content)!;
        _healthCheckResult!.Status.Should().Be(HealthCheckStatuses.Degraded);
    }

    [Then("the cow service dependency should report degraded")]
    public void ThenTheCowServiceDependencyShouldReportDegraded()
    {
        _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.CowService);
        _healthCheckResult.Results[HealthCheckNames.CowService].Status.Should().Be(HealthCheckStatuses.Degraded);
    }

    [Then("the supplier service dependency should report degraded")]
    public void ThenTheSupplierServiceDependencyShouldReportDegraded()
    {
        _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.SupplierService);
        _healthCheckResult.Results[HealthCheckNames.SupplierService].Status.Should().Be(HealthCheckStatuses.Degraded);
    }

    [Then("the kitchen service dependency should report degraded with a status code description")]
    public void ThenTheKitchenServiceDependencyShouldReportDegradedWithAStatusCodeDescription()
    {
        _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.KitchenService);
        var entry = _healthCheckResult.Results[HealthCheckNames.KitchenService];
        entry.Status.Should().Be(HealthCheckStatuses.Degraded);
        entry.Description.Should().Contain("503");
    }
}
