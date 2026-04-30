using System.Net;
using System.Text;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Pancakes;

#pragma warning disable CS1998
public partial class Pancakes__Content_Negotiation_Feature : BaseFixture
{
    private HttpResponseMessage? _response;
    private string _contentType = null!;

    #region Given

    private async Task A_pancake_request_with_content_type(string contentType)
    {
        _contentType = contentType;
    }

    #endregion

    #region When

    private async Task The_pancakes_are_prepared()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Pancakes)
        {
            Content = new StringContent("{}", Encoding.UTF8, _contentType)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _response = await Client.SendAsync(request);
    }

    #endregion

    #region Then

    private async Task The_response_should_indicate_unsupported_media_type()
        => _response!.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);

    #endregion
}
