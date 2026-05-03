using System.Net;
using System.Text;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Waffles;

#pragma warning disable CS1998
public class Waffles_Content_Negotiation_Tests : BaseFixture
{
    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("text/html")]
    public async Task Sending_request_with_unsupported_content_type_should_return_unsupported_media_type(string contentType)
    {
        // Given a waffle request with an unsupported content type
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Waffles)
        {
            Content = new StringContent("{}", Encoding.UTF8, contentType)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);

        // When the waffles are prepared
        var response = await Client.SendAsync(request);

        // Then the response should indicate unsupported media type
        Track.That(() => response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType));
    }
}
