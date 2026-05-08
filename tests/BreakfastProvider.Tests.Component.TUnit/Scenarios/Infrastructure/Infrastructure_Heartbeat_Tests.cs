using System.Net;
using BreakfastProvider.Api;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Infrastructure;

public class Infrastructure_Heartbeat_Tests : BaseFixture
{
    [Test]
    [HappyPath]
    public async Task Heartbeat_endpoint_should_return_a_running_message()
    {
        // When the heartbeat endpoint is called
        var response = await Client.GetAsync($"/{Endpoints.Heartbeat}");

        // Then the response should indicate the service is running
        await response.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHeartbeatResponse>(content);
        await result.Should().NotBeNull();
        await result!.Status.Should().BeEqualTo(Documentation.HeartbeatStatus);
    }

    private record TestHeartbeatResponse(string Status);
}
