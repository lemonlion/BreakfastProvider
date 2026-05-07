using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using Microsoft.Extensions.DependencyInjection;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Infrastructure;

public class Infrastructure_Downstream_Error_Health_Check_Tests : BaseFixture
{
    private TestHealthCheckResponse? _result;

    public Infrastructure_Downstream_Error_Health_Check_Tests() : base(delayAppCreation: true) { }

    [Fact]
    public void Health_check_should_report_degraded_when_downstream_service_returns_non_success_status()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.The_kitchen_service_health_check_is_configured_to_use_a_failing_endpoint())
            .When(x => x.The_health_check_endpoint_is_called())
            .Then(x => x.The_response_should_indicate_degraded_status())
            .And(x => x.The_kitchen_service_should_report_degraded_with_status_code_description())
            .BDDfy();
    }

    #region Steps

    private void The_kitchen_service_health_check_is_configured_to_use_a_failing_endpoint()
    {
        CreateAppAndClient(additionalServices: services =>
        {
            services.ReplaceHealthCheckWithFailingEndpoint(HealthCheckNames.KitchenService, "health-degraded");

            if (!Settings.RunWithAnInMemoryDatabase)
                services.ReplaceCosmosDbHealthCheckWithNoOp();
        });
    }

    private async Task The_health_check_endpoint_is_called()
    {
        var response = await Client.GetAsync(Endpoints.Health);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        _result = Json.Deserialize<TestHealthCheckResponse>(content)!;
    }

    private void The_response_should_indicate_degraded_status()
    {
        _result.Should().NotBeNull();
        _result!.Status.Should().Be(HealthCheckStatuses.Degraded);
    }

    private void The_kitchen_service_should_report_degraded_with_status_code_description()
    {
        _result!.Results.Should().ContainKey(HealthCheckNames.KitchenService);
        var kitchenEntry = _result.Results[HealthCheckNames.KitchenService];
        kitchenEntry.Status.Should().Be(HealthCheckStatuses.Degraded);
        kitchenEntry.Description.Should().Contain("503");
    }

    #endregion
}
