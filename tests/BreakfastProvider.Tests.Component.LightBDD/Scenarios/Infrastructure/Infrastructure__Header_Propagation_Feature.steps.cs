using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

#pragma warning disable CS1998
public partial class Infrastructure__Header_Propagation_Feature : BaseFixture
{
    private readonly DownstreamRequestSteps _downstreamSteps;

    private string _correlationId = null!;
    private HttpResponseMessage? _response;

    // Use an isolated WebApplicationFactory so the IMemoryCache is not shared
    // with parallel tests — prevents other tests' GET /menu from populating
    // the cache between our DELETE /menu/cache and GET /menu steps.
    public Infrastructure__Header_Propagation_Feature() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "HeaderPropagation" });
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    #region Given

    private async Task A_request_with_a_known_correlation_id()
    {
        _correlationId = Guid.NewGuid().ToString();
    }

    private async Task The_menu_cache_is_cleared()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, Endpoints.MenuCache);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        await Client.SendAsync(request);
    }

    #endregion

    #region When

    private async Task Milk_is_requested_from_the_milk_endpoint()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Milk);
        request.Headers.Add(CustomHeaders.CorrelationId, _correlationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _response = await Client.SendAsync(request);
    }

    private async Task The_menu_is_requested()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.CorrelationId, _correlationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _response = await Client.SendAsync(request);
    }

    #endregion

    #region Then

    private async Task The_cow_service_should_have_received_the_correlation_id()
        => _downstreamSteps.AssertDownstreamReceivedCorrelationId(ServiceNames.CowService, _correlationId);

    private async Task The_supplier_service_should_have_received_the_correlation_id()
        => _downstreamSteps.AssertDownstreamReceivedCorrelationId(ServiceNames.SupplierService, _correlationId);

    #endregion
}
