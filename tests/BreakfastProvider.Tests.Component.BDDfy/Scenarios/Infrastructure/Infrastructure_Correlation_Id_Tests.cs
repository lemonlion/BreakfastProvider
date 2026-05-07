using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Infrastructure;

public class Infrastructure_Correlation_Id_Tests : BaseFixture
{
    private string _knownCorrelationId = null!;
    private HttpResponseMessage? _response;

    [Fact]
    [HappyPath]
    public void Request_with_correlation_id_should_return_same_id_in_response()
    {
        this.Given(x => x.A_request_with_a_known_correlation_id())
            .When(x => x.The_request_is_sent_to_the_menu_endpoint())
            .Then(x => x.The_response_should_contain_the_same_correlation_id())
            .BDDfy();
    }

    [Fact]
    public void Request_without_correlation_id_should_have_one_generated_in_response()
    {
        this.When(x => x.A_request_without_a_correlation_id_is_sent_to_the_menu_endpoint())
            .Then(x => x.The_response_should_contain_a_generated_correlation_id())
            .BDDfy();
    }

    #region Steps

    private void A_request_with_a_known_correlation_id()
    {
        _knownCorrelationId = Guid.NewGuid().ToString();
    }

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

    private void The_response_should_contain_the_same_correlation_id()
    {
        var responseContainsCorrelationIdHeader = _response!.Headers.TryGetValues(CustomHeaders.CorrelationId, out var values);
        responseContainsCorrelationIdHeader.Should().BeTrue();
        values!.First().Should().Be(_knownCorrelationId);
    }

    private void The_response_should_contain_a_generated_correlation_id()
    {
        var responseContainsCorrelationIdHeader = _response!.Headers.TryGetValues(CustomHeaders.CorrelationId, out var values);
        responseContainsCorrelationIdHeader.Should().BeTrue();
        values!.First().Should().NotBeNullOrEmpty();
    }

    #endregion
}
