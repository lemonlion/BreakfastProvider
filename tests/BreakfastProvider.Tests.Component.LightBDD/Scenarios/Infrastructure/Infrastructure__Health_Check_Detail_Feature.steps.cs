using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

#pragma warning disable CS1998
public partial class Infrastructure__Health_Check_Detail_Feature : BaseFixture
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

    private async Task<CompositeStep> The_health_check_response_should_contain_detailed_entries()
    {
        return Sub.Steps(
            _ => The_health_check_response_status_should_be_ok(),
            _ => The_health_check_response_should_be_valid_json(),
            _ => Each_entry_should_have_a_status(),
            _ => Each_downstream_entry_should_have_a_description(),
            _ => Each_entry_should_have_a_data_object());
    }

    private async Task The_health_check_response_status_should_be_ok()
        => Track.That(() => _healthResponse!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_health_check_response_should_be_valid_json()
    {
        var content = await _healthResponse!.Content.ReadAsStringAsync();
        _healthCheckResult = Json.Deserialize<TestHealthCheckResponse>(content)!;
        Track.That(() => _healthCheckResult.Should().NotBeNull());
    }

    private async Task Each_entry_should_have_a_status()
    {
        foreach (var entry in _healthCheckResult!.Results)
        {
            var healthCheckEntryStatus = entry.Value.Status;
            Track.That(() => healthCheckEntryStatus.Should().NotBeNullOrEmpty(
                $"health check entry '{entry.Key}' should have a status"));
        }
    }

    private async Task Each_downstream_entry_should_have_a_description()
    {
        string[] downstreamChecks =
        [
            HealthCheckNames.CowService,
            HealthCheckNames.GoatService,
            HealthCheckNames.SupplierService,
            HealthCheckNames.KitchenService
        ];

        foreach (var checkName in downstreamChecks)
        {
            Track.That(() => _healthCheckResult!.Results.Should().ContainKey(checkName));
            var healthCheckDescription = _healthCheckResult.Results[checkName].Description;
            Track.That(() => healthCheckDescription.Should().NotBeNullOrEmpty(
                $"health check entry '{checkName}' should have a description"));
        }
    }

    private async Task Each_entry_should_have_a_data_object()
    {
        foreach (var entry in _healthCheckResult!.Results)
        {
            var healthCheckEntryData = entry.Value.Data;
            Track.That(() => healthCheckEntryData.Should().NotBeNull(
                $"health check entry '{entry.Key}' should have a data object"));
        }
    }

    #endregion
}
