using System.Net;
using System.Text;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Pancakes;

public class Pancakes_Content_Negotiation_Tests : BaseFixture
{
    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("text/html")]
    public async Task Request_with_unsupported_content_type_should_return_unsupported_media_type(string contentType)
    {
        // Given a pancake request with an unsupported content type
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Pancakes)
        {
            Content = new StringContent("{}", Encoding.UTF8, contentType)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);

        // When the pancakes are prepared
        var response = await Client.SendAsync(request);

        // Then the response should indicate unsupported media type
        Track.That(() => response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType));
    }
}
