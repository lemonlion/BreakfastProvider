using System.Net;
using System.Text;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Pancakes;

[Binding]
public class PancakeContentNegotiationSteps(AppManager appManager)
{
    private HttpResponseMessage? _response;
    private string _contentType = null!;

    [Given(@"a pancake request with content type ""(.*)""")]
    public void GivenAPancakeRequestWithContentType(string contentType)
    {
        _contentType = contentType;
    }

    [When("the pancakes are prepared with the given content type")]
    public async Task WhenThePancakesArePreparedWithTheGivenContentType()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Pancakes)
        {
            Content = new StringContent("{}", Encoding.UTF8, _contentType)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _response = await appManager.Client.SendAsync(request);
    }

    [Then("the response should indicate unsupported media type")]
    public void ThenTheResponseShouldIndicateUnsupportedMediaType()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }
}
