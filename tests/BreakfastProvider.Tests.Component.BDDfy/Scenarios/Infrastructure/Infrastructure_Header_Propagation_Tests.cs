using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Infrastructure;

public class Infrastructure_Header_Propagation_Tests : BaseFixture
{
    private readonly DownstreamRequestSteps _downstreamSteps;
    private string _correlationId = null!;

    public Infrastructure_Header_Propagation_Tests() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "HeaderPropagation" });
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public void Request_with_correlation_id_should_forward_it_to_cow_service()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.A_request_with_a_known_correlation_id())
            .When(x => x.Milk_is_requested_from_the_milk_endpoint())
            .Then(x => x.The_cow_service_should_have_received_the_correlation_id())
            .BDDfy();
    }

    [Fact]
    public void Request_with_correlation_id_should_forward_it_to_supplier_service()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.A_request_with_a_known_correlation_id())
            .And(x => x.The_menu_cache_is_cleared())
            .When(x => x.The_menu_is_requested())
            .Then(x => x.The_supplier_service_should_have_received_the_correlation_id())
            .BDDfy();
    }

    #region Steps

    private void A_request_with_a_known_correlation_id()
    {
        _correlationId = Guid.NewGuid().ToString();
    }

    private async Task Milk_is_requested_from_the_milk_endpoint()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Milk);
        request.Headers.Add(CustomHeaders.CorrelationId, _correlationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        await Client.SendAsync(request);
    }

    private async Task The_menu_cache_is_cleared()
    {
        var clearRequest = new HttpRequestMessage(HttpMethod.Delete, Endpoints.MenuCache);
        clearRequest.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        await Client.SendAsync(clearRequest);
    }

    private async Task The_menu_is_requested()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.CorrelationId, _correlationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        await Client.SendAsync(request);
    }

    private void The_cow_service_should_have_received_the_correlation_id()
    {
        _downstreamSteps.AssertDownstreamReceivedCorrelationId(ServiceNames.CowService, _correlationId);
    }

    private void The_supplier_service_should_have_received_the_correlation_id()
    {
        _downstreamSteps.AssertDownstreamReceivedCorrelationId(ServiceNames.SupplierService, _correlationId);
    }

    #endregion
}
