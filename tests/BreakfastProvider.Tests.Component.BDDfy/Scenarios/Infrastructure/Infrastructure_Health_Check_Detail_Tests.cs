using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Infrastructure;

public class Infrastructure_Health_Check_Detail_Tests : BaseFixture
{
    private TestHealthCheckResponse? _result;

    [Fact]
    public void Health_check_response_should_include_description_and_data_for_each_entry()
    {
        this.When(x => x.The_health_check_endpoint_is_called())
            .Then(x => x.Each_entry_should_have_a_status())
            .And(x => x.Each_downstream_entry_should_have_a_description())
            .And(x => x.Each_entry_should_have_a_data_object())
            .BDDfy();
    }

    #region Steps

    private async Task The_health_check_endpoint_is_called()
    {
        var response = await Client.GetAsync(Endpoints.Health);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        _result = Json.Deserialize<TestHealthCheckResponse>(content)!;
        _result.Should().NotBeNull();
    }

    private void Each_entry_should_have_a_status()
    {
        foreach (var entry in _result!.Results)
        {
            entry.Value.Status.Should().NotBeNullOrEmpty(
                $"health check entry '{entry.Key}' should have a status");
        }
    }

    private void Each_downstream_entry_should_have_a_description()
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
            _result!.Results.Should().ContainKey(checkName);
            _result.Results[checkName].Description.Should().NotBeNullOrEmpty(
                $"health check entry '{checkName}' should have a description");
        }
    }

    private void Each_entry_should_have_a_data_object()
    {
        foreach (var entry in _result!.Results)
        {
            entry.Value.Data.Should().NotBeNull(
                $"health check entry '{entry.Key}' should have a data object");
        }
    }

    #endregion
}
