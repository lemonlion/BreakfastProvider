using System.Net;
using BreakfastProvider.Api;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Infrastructure;

public class Infrastructure_Heartbeat_Tests : BaseFixture
{
    [Fact]
    [HappyPath]
    public async Task Heartbeat_endpoint_should_return_a_running_message()
    {
        // When the heartbeat endpoint is called
        var response = await Client.GetAsync($"/{Endpoints.Heartbeat}");

        // Then the response should indicate the service is running
        Track.That(() => response.StatusCode.Should().Be(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHeartbeatResponse>(content);
        Track.That(() => result.Should().NotBeNull());
        Track.That(() => result!.Status.Should().Be(Documentation.HeartbeatStatus));
    }

    private record TestHeartbeatResponse(string Status);
}
