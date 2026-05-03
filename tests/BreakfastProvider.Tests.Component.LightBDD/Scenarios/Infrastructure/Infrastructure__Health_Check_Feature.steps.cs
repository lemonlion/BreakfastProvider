using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

#pragma warning disable CS1998
public partial class Infrastructure__Health_Check_Feature : BaseFixture
{
    private HttpResponseMessage? _healthResponse;
    private TestHealthCheckResponse? _healthCheckResult;

    #region Given
    #endregion

    #region When

    private async Task The_health_check_endpoint_is_called()
    {
        _healthResponse = await Client.GetAsync(Endpoints.Health);
    }

    #endregion

    #region Then

    private async Task<CompositeStep> The_health_check_response_should_indicate_healthy_with_all_dependencies()
    {
        return Sub.Steps(
            _ => The_health_check_response_status_should_be_ok(),
            _ => The_health_check_response_should_be_valid_json(),
            _ => The_overall_status_should_be_healthy(),
            _ => The_response_should_include_cow_service_check(),
            _ => The_response_should_include_goat_service_check(),
            _ => The_response_should_include_supplier_service_check(),
            _ => The_response_should_include_kitchen_service_check(),
            _ => The_response_should_include_cosmos_db_check(),
            _ => The_response_should_include_kafka_check());
    }

    private async Task The_health_check_response_status_should_be_ok()
        => Track.That(() => _healthResponse!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_health_check_response_should_be_valid_json()
    {
        var content = await _healthResponse!.Content.ReadAsStringAsync();
        _healthCheckResult = Json.Deserialize<TestHealthCheckResponse>(content)!;
        Track.That(() => _healthCheckResult.Should().NotBeNull());
    }

    private async Task The_overall_status_should_be_healthy()
        => Track.That(() => _healthCheckResult!.Status.Should().Be(HealthCheckStatuses.Healthy));

    private async Task The_response_should_include_cow_service_check()
        => Track.That(() => _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.CowService));

    private async Task The_response_should_include_goat_service_check()
        => Track.That(() => _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.GoatService));

    private async Task The_response_should_include_supplier_service_check()
        => Track.That(() => _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.SupplierService));

    private async Task The_response_should_include_kitchen_service_check()
        => Track.That(() => _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.KitchenService));

    private async Task The_response_should_include_cosmos_db_check()
        => Track.That(() => _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.CosmosDb));

    private async Task The_response_should_include_kafka_check()
        => Track.That(() => _healthCheckResult!.Results.Should().ContainKey(HealthCheckNames.Kafka));

    #endregion
}
