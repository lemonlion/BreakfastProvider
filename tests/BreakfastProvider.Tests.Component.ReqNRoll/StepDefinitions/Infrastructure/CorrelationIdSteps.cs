using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Infrastructure;

[Binding]
public class CorrelationIdSteps(AppManager appManager, DownstreamRequestSteps downstreamSteps)
{
    private string _knownCorrelationId = null!;
    private HttpResponseMessage? _response;

    [Given("a request with a known correlation id")]
    public void GivenARequestWithAKnownCorrelationId()
    {
        _knownCorrelationId = Guid.NewGuid().ToString();
    }

    [When("the request is sent to the menu endpoint")]
    public async Task WhenTheRequestIsSentToTheMenuEndpoint()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.CorrelationId, _knownCorrelationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _response = await appManager.Client.SendAsync(request);
    }

    [When("a request without a correlation id is sent to the menu endpoint")]
    public async Task WhenARequestWithoutACorrelationIdIsSentToTheMenuEndpoint()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _response = await appManager.Client.SendAsync(request);
    }

    [Then("the response should contain the same correlation id")]
    public void ThenTheResponseShouldContainTheSameCorrelationId()
    {
        _response!.Headers.TryGetValues(CustomHeaders.CorrelationId, out var values).Should().BeTrue();
        values!.First().Should().Be(_knownCorrelationId);
    }

    [Then("the response should contain a generated correlation id")]
    public void ThenTheResponseShouldContainAGeneratedCorrelationId()
    {
        _response!.Headers.TryGetValues(CustomHeaders.CorrelationId, out var values).Should().BeTrue();
        values!.First().Should().NotBeNullOrEmpty();
    }

    // --- Header Propagation ---
    [Given("the menu cache is cleared")]
    public async Task GivenTheMenuCacheIsCleared()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, Endpoints.MenuCache);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        await appManager.Client.SendAsync(request);
    }

    [When("the menu is requested with the correlation id")]
    public async Task WhenTheMenuIsRequestedWithTheCorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.CorrelationId, _knownCorrelationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _response = await appManager.Client.SendAsync(request);
    }

    [When("milk is requested with the correlation id")]
    public async Task WhenMilkIsRequestedWithTheCorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Milk);
        request.Headers.Add(CustomHeaders.CorrelationId, _knownCorrelationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _response = await appManager.Client.SendAsync(request);
    }

    [Then("the cow service should have received the correlation id")]
    public void ThenTheCowServiceShouldHaveReceivedTheCorrelationId()
        => downstreamSteps.AssertDownstreamReceivedCorrelationId(ServiceNames.CowService, _knownCorrelationId);

    [Then("the supplier service should have received the correlation id")]
    public void ThenTheSupplierServiceShouldHaveReceivedTheCorrelationId()
        => downstreamSteps.AssertDownstreamReceivedCorrelationId(ServiceNames.SupplierService, _knownCorrelationId);
}
