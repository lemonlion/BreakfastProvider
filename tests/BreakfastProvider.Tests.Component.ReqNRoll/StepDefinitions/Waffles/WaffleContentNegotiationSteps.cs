using System.Net;
using System.Text;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Waffles;

[Binding]
public class WaffleContentNegotiationSteps(AppManager appManager)
{
    private HttpResponseMessage? _response;
    private string _contentType = null!;

    [Given(@"a waffle request with content type ""(.*)""")]
    public void GivenAWaffleRequestWithContentType(string contentType)
    {
        _contentType = contentType;
    }

    [When("the waffles are prepared with the given content type")]
    public async Task WhenTheWafflesArePreparedWithTheGivenContentType()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Waffles)
        {
            Content = new StringContent("{}", Encoding.UTF8, _contentType)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _response = await appManager.Client.SendAsync(request);
    }

    [Then("the waffle response should indicate unsupported media type")]
    public void ThenTheWaffleResponseShouldIndicateUnsupportedMediaType()
    {
        Track.That(() => _response!.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType));
    }
}
