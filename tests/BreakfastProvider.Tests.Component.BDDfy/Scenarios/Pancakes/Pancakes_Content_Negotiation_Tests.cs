using System.Net;
using System.Text;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Pancakes;

public class Pancakes_Content_Negotiation_Tests : BaseFixture
{
    private string _contentType = null!;
    private HttpResponseMessage _response = null!;

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("text/html")]
    public void Request_with_unsupported_content_type_should_return_unsupported_media_type(string contentType)
    {
        _contentType = contentType;

        this.Given(x => x.A_pancake_request_with_an_unsupported_content_type())
            .When(x => x.The_request_is_sent())
            .Then(x => x.The_response_should_indicate_unsupported_media_type())
            .BDDfy();
    }

    #region Steps

    private void A_pancake_request_with_an_unsupported_content_type()
    {
        // Step is a marker — content type set via _contentType field
    }

    private async Task The_request_is_sent()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Pancakes)
        {
            Content = new StringContent("{}", Encoding.UTF8, _contentType)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _response = await Client.SendAsync(request);
    }

    private void The_response_should_indicate_unsupported_media_type()
    {
        _response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    #endregion
}
