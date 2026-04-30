using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using Microsoft.Extensions.DependencyInjection;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

#pragma warning disable CS1998
public partial class Infrastructure__Degraded_Health_Check_Feature : BaseFixture
{
    private readonly List<string> _degradedChecks = [];
    private bool _appCreated;

    private HttpResponseMessage? _healthResponse;
    private TestHealthCheckResponse? _healthCheckResult;

    public Infrastructure__Degraded_Health_Check_Feature() : base(delayAppCreation: true) { }

    private void EnsureAppCreated()
    {
        if (_appCreated) return;

        Action<IServiceCollection> additionalServices = services =>
        {
            foreach (var checkName in _degradedChecks)
            {
                services.ReplaceHealthCheckWithDegraded(checkName, $"{checkName} is unreachable (simulated for test).");
            }

            // Docker mode: replace infrastructure health checks with no-ops so that
            // transient Cosmos/Kafka instability doesn't override the downstream
            // degraded status we're asserting on.
            if (!Settings.RunWithAnInMemoryDatabase)
                services.ReplaceCosmosDbHealthCheckWithNoOp();

            if (!Settings.RunWithAnInMemoryKafkaBroker)
                services.ReplaceKafkaHealthCheckWithNoOp();
        };

        CreateAppAndClient(additionalServices: additionalServices);
        _appCreated = true;
    }

    #region Given

    private async Task The_cow_service_is_configured_to_be_unreachable()
    {
        _degradedChecks.Add(HealthCheckNames.CowService);
    }

    private async Task The_supplier_service_is_configured_to_be_unreachable()
    {
        _degradedChecks.Add(HealthCheckNames.SupplierService);
    }

    #endregion

    #region When

    private async Task The_health_check_endpoint_is_called()
    {
        EnsureAppCreated();
        _healthResponse = await Client.GetAsync(Endpoints.Health);
    }

    #endregion

    #region Then

    private async Task<CompositeStep> The_health_check_response_should_indicate_a_degraded_status()
    {
        return Sub.Steps(
            _ => The_health_check_response_status_should_be_ok(),
            _ => The_health_check_response_should_be_valid_json(),
            _ => The_overall_status_should_be_degraded());
    }

    private async Task The_health_check_response_status_should_be_ok()
        => _healthResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_health_check_response_should_be_valid_json()
    {
        var content = await _healthResponse!.Content.ReadAsStringAsync();
        _healthCheckResult = Json.Deserialize<TestHealthCheckResponse>(content)!;
        _healthCheckResult.Should().NotBeNull();
    }

    private async Task The_overall_status_should_be_degraded()
        => _healthCheckResult!.Status.Should().Be(HealthCheckStatuses.Degraded);

    private async Task The_cow_service_dependency_should_report_degraded()
    {
        _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.CowService);
        _healthCheckResult.Results[HealthCheckNames.CowService].Status.Should().Be(HealthCheckStatuses.Degraded);
    }

    private async Task The_supplier_service_dependency_should_report_degraded()
    {
        _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.SupplierService);
        _healthCheckResult.Results[HealthCheckNames.SupplierService].Status.Should().Be(HealthCheckStatuses.Degraded);
    }

    #endregion
}
