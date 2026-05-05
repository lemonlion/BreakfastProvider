using BreakfastProvider.Tests.Component.Shared.Constants;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Infrastructure;

public class Infrastructure_Correlation_Id_Tests : BaseFixture
{
    [Test]
    [HappyPath]
    public async Task Request_with_correlation_id_should_return_same_id_in_response()
    {
        // Given a request with a known correlation id
        var knownCorrelationId = Guid.NewGuid().ToString();

        // When the request is sent to the menu endpoint
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.CorrelationId, knownCorrelationId);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        var response = await Client.SendAsync(request);

        // Then the response should contain the same correlation id
        var responseContainsCorrelationIdHeader = response.Headers.TryGetValues(CustomHeaders.CorrelationId, out var values);
        responseContainsCorrelationIdHeader.Should().BeTrue();
        var firstCorrelationIdHeaderValue = values!.First();
        firstCorrelationIdHeaderValue.Should().Be(knownCorrelationId);
    }

    [Test]
    public async Task Request_without_correlation_id_should_have_one_generated_in_response()
    {
        // When a request without a correlation id is sent to the menu endpoint
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        var response = await Client.SendAsync(request);

        // Then the response should contain a generated correlation id
        var responseContainsCorrelationIdHeader = response.Headers.TryGetValues(CustomHeaders.CorrelationId, out var values);
        responseContainsCorrelationIdHeader.Should().BeTrue();
        var firstCorrelationIdHeaderValue = values!.First();
        firstCorrelationIdHeaderValue.Should().NotBeNullOrEmpty();
    }
}
