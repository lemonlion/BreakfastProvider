using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Constants;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Infrastructure;

public class Infrastructure_Header_Propagation_Tests : BaseFixture
{
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Infrastructure_Header_Propagation_Tests() : base(delayAppCreation: true)
    {
        CreateAppAndClient(new Dictionary<string, string?> { ["TestIsolation"] = "HeaderPropagation" });
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Request_with_correlation_id_should_forward_it_to_cow_service()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given a request with a known correlation id
        var correlationId = Guid.NewGuid().ToString();

        // When milk is requested from the milk endpoint
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Milk);
        request.Headers.Add(CustomHeaders.CorrelationId, correlationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        await Client.SendAsync(request);

        // Then the cow service should have received the correlation id
        _downstreamSteps.AssertDownstreamReceivedCorrelationId(ServiceNames.CowService, correlationId);
    }

    [Fact]
    public async Task Request_with_correlation_id_should_forward_it_to_supplier_service()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given a request with a known correlation id
        var correlationId = Guid.NewGuid().ToString();

        // And the menu cache is cleared
        var clearRequest = new HttpRequestMessage(HttpMethod.Delete, Endpoints.MenuCache);
        clearRequest.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        await Client.SendAsync(clearRequest);

        // When the menu is requested
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.CorrelationId, correlationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        await Client.SendAsync(request);

        // Then the supplier service should have received the correlation id
        _downstreamSteps.AssertDownstreamReceivedCorrelationId(ServiceNames.SupplierService, correlationId);
    }
}
