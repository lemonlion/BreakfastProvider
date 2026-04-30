using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

#pragma warning disable CS1998
public partial class Infrastructure__Downstream_Error_Health_Check_Feature : BaseFixture
{
    private bool _appCreated;

    private HttpResponseMessage? _healthResponse;
    private TestHealthCheckResponse? _healthCheckResult;

    public Infrastructure__Downstream_Error_Health_Check_Feature() : base(delayAppCreation: true) { }

    private void EnsureAppCreated()
    {
        if (_appCreated) return;

        CreateAppAndClient(additionalServices: services =>
        {
            services.ReplaceHealthCheckWithFailingEndpoint(HealthCheckNames.KitchenService, "health-degraded");

            // Docker mode: this test only exercises the /health endpoint and doesn't
            // need Cosmos queries. Replace the Cosmos health check with a no-op so it
            // doesn't report Unhealthy and mask the Kitchen Service degraded status.
            // IMPORTANT: Do NOT call UseInMemoryDatabase() here — it sets a static
            // FeedIteratorFactory that would poison all other tests in the process.
            if (!Settings.RunWithAnInMemoryDatabase)
            {
                services.ReplaceCosmosDbHealthCheckWithNoOp();
            }
        });

        _appCreated = true;
    }

    #region Given

    private async Task The_kitchen_service_health_check_is_configured_to_use_a_failing_endpoint()
    {
        // Configuration is applied during app creation — nothing else to do here.
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

    private async Task The_kitchen_service_dependency_should_report_degraded_with_a_status_code_description()
    {
        _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.KitchenService);

        var kitchenEntry = _healthCheckResult.Results[HealthCheckNames.KitchenService];
        kitchenEntry.Status.Should().Be(HealthCheckStatuses.Degraded);
        kitchenEntry.Description.Should().Contain("503");
    }

    #endregion
}
