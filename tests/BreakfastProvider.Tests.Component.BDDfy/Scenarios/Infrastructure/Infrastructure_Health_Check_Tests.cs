using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Infrastructure;

public class Infrastructure_Health_Check_Tests : BaseFixture
{
    private HttpResponseMessage? _response;
    private TestHealthCheckResponse? _result;

    [Fact]
    [HappyPath]
    public void Health_check_endpoint_should_return_healthy_status_with_all_dependency_details()
    {
        this.When(x => x.The_health_check_endpoint_is_called())
            .Then(x => x.The_response_should_indicate_healthy())
            .And(x => x.All_dependency_health_checks_should_be_present())
            .BDDfy();
    }

    #region Steps

    private async Task The_health_check_endpoint_is_called()
    {
        _response = await Client.GetAsync(Endpoints.Health);
        var content = await _response.Content.ReadAsStringAsync();
        _result = Json.Deserialize<TestHealthCheckResponse>(content)!;
    }

    private void The_response_should_indicate_healthy()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _result.Should().NotBeNull();
        _result!.Status.Should().Be(HealthCheckStatuses.Healthy);
    }

    private void All_dependency_health_checks_should_be_present()
    {
        _result!.Results.Should().ContainKey(HealthCheckNames.CowService);
        _result.Results.Should().ContainKey(HealthCheckNames.GoatService);
        _result.Results.Should().ContainKey(HealthCheckNames.SupplierService);
        _result.Results.Should().ContainKey(HealthCheckNames.KitchenService);
        _result.Results.Should().ContainKey(HealthCheckNames.CosmosDb);
        _result.Results.Should().ContainKey(HealthCheckNames.Kafka);
    }

    #endregion
}
