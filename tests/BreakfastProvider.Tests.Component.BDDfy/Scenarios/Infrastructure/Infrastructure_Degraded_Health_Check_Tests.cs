using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Infrastructure;
using BreakfastProvider.Tests.Component.Shared.Util;
using Microsoft.Extensions.DependencyInjection;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Infrastructure;

public class Infrastructure_Degraded_Health_Check_Tests : BaseFixture
{
    private HttpResponseMessage? _response;
    private TestHealthCheckResponse? _result;

    public Infrastructure_Degraded_Health_Check_Tests() : base(delayAppCreation: true) { }

    [Fact]
    public void Health_check_should_report_degraded_when_cow_service_is_unavailable()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.The_cow_service_is_configured_to_be_unreachable())
            .When(x => x.The_health_check_endpoint_is_called())
            .Then(x => x.The_response_should_indicate_degraded_status())
            .And(x => x.The_cow_service_dependency_should_report_degraded())
            .BDDfy();
    }

    [Fact]
    public void Health_check_should_report_degraded_when_multiple_downstream_services_are_unavailable()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.The_cow_service_and_supplier_service_are_configured_to_be_unreachable())
            .When(x => x.The_health_check_endpoint_is_called())
            .Then(x => x.The_response_should_indicate_degraded_status())
            .And(x => x.The_cow_service_dependency_should_report_degraded())
            .And(x => x.The_supplier_service_dependency_should_report_degraded())
            .BDDfy();
    }

    #region Steps

    private void The_cow_service_is_configured_to_be_unreachable()
    {
        CreateAppAndClient(additionalServices: services =>
        {
            services.ReplaceHealthCheckWithDegraded(HealthCheckNames.CowService,
                $"{HealthCheckNames.CowService} is unreachable (simulated for test).");

            if (!Settings.RunWithAnInMemoryDatabase)
                services.ReplaceCosmosDbHealthCheckWithNoOp();
            if (!Settings.RunWithAnInMemoryKafkaBroker)
                services.ReplaceKafkaHealthCheckWithNoOp();
        });
    }

    private void The_cow_service_and_supplier_service_are_configured_to_be_unreachable()
    {
        CreateAppAndClient(additionalServices: services =>
        {
            services.ReplaceHealthCheckWithDegraded(HealthCheckNames.CowService,
                $"{HealthCheckNames.CowService} is unreachable (simulated for test).");
            services.ReplaceHealthCheckWithDegraded(HealthCheckNames.SupplierService,
                $"{HealthCheckNames.SupplierService} is unreachable (simulated for test).");

            if (!Settings.RunWithAnInMemoryDatabase)
                services.ReplaceCosmosDbHealthCheckWithNoOp();
            if (!Settings.RunWithAnInMemoryKafkaBroker)
                services.ReplaceKafkaHealthCheckWithNoOp();
        });
    }

    private async Task The_health_check_endpoint_is_called()
    {
        _response = await Client.GetAsync(Endpoints.Health);
        var content = await _response.Content.ReadAsStringAsync();
        _result = Json.Deserialize<TestHealthCheckResponse>(content)!;
    }

    private void The_response_should_indicate_degraded_status()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _result.Should().NotBeNull();
        _result!.Status.Should().Be(HealthCheckStatuses.Degraded);
    }

    private void The_cow_service_dependency_should_report_degraded()
    {
        _result!.Results.Should().ContainKey(HealthCheckNames.CowService);
        _result.Results[HealthCheckNames.CowService].Status.Should().Be(HealthCheckStatuses.Degraded);
    }

    private void The_supplier_service_dependency_should_report_degraded()
    {
        _result!.Results.Should().ContainKey(HealthCheckNames.SupplierService);
        _result.Results[HealthCheckNames.SupplierService].Status.Should().Be(HealthCheckStatuses.Degraded);
    }

    #endregion
}
