using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

#pragma warning disable CS1998
public partial class Infrastructure__Correlation_Id_Feature : BaseFixture
{
    private string _knownCorrelationId = null!;
    private HttpResponseMessage? _response;

    #region Given

    private async Task A_request_with_a_known_correlation_id()
    {
        _knownCorrelationId = Guid.NewGuid().ToString();
    }

    #endregion

    #region When

    private async Task The_request_is_sent_to_the_menu_endpoint()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.CorrelationId, _knownCorrelationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _response = await Client.SendAsync(request);
    }

    private async Task A_request_without_a_correlation_id_is_sent_to_the_menu_endpoint()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _response = await Client.SendAsync(request);
    }

    #endregion

    #region Then

    private async Task The_response_should_contain_the_same_correlation_id()
    {
        _response!.Headers.TryGetValues(CustomHeaders.CorrelationId, out var values).Should().BeTrue();
        values!.First().Should().Be(_knownCorrelationId);
    }

    private async Task The_response_should_contain_a_generated_correlation_id()
    {
        _response!.Headers.TryGetValues(CustomHeaders.CorrelationId, out var values).Should().BeTrue();
        values!.First().Should().NotBeNullOrEmpty();
    }

    #endregion
}
