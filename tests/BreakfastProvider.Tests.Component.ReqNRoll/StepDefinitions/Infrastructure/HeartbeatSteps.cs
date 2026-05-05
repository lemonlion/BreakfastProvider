using System.Net;
using BreakfastProvider.Api;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Infrastructure;

[Binding]
public class HeartbeatSteps(AppManager appManager)
{
    private HttpResponseMessage? _heartbeatResponse;

    [When("the heartbeat endpoint is called")]
    public async Task WhenTheHeartbeatEndpointIsCalled()
    {
        _heartbeatResponse = await appManager.Client.GetAsync($"/{Endpoints.Heartbeat}");
    }

    [Then("the heartbeat response should indicate the service is running")]
    public async Task ThenTheHeartbeatResponseShouldIndicateTheServiceIsRunning()
    {
        _heartbeatResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await _heartbeatResponse!.Content.ReadAsStringAsync();
        var result = Json.Deserialize<HeartbeatResult>(content);
        result.Should().NotBeNull();
        result!.Status.Should().Be(Documentation.HeartbeatStatus);
    }

    private record HeartbeatResult(string Status);
}
