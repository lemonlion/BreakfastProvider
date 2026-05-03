using BreakfastProvider.Tests.Component.Shared.Constants;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Infrastructure;

public class Infrastructure_Correlation_Id_Tests : BaseFixture
{
    [Fact]
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
        Track.That(() => responseContainsCorrelationIdHeader.Should().BeTrue());
        var firstCorrelationIdHeaderValue = values!.First();
        Track.That(() => firstCorrelationIdHeaderValue.Should().Be(knownCorrelationId));
    }

    [Fact]
    public async Task Request_without_correlation_id_should_have_one_generated_in_response()
    {
        // When a request without a correlation id is sent to the menu endpoint
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Menu);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        var response = await Client.SendAsync(request);

        // Then the response should contain a generated correlation id
        var responseContainsCorrelationIdHeader = response.Headers.TryGetValues(CustomHeaders.CorrelationId, out var values);
        Track.That(() => responseContainsCorrelationIdHeader.Should().BeTrue());
        var firstCorrelationIdHeaderValue = values!.First();
        Track.That(() => firstCorrelationIdHeaderValue.Should().NotBeNullOrEmpty());
    }
}
