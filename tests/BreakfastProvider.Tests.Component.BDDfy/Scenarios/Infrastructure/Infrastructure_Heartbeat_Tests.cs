using System.Net;
using BreakfastProvider.Api;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Infrastructure;

public class Infrastructure_Heartbeat_Tests : BaseFixture
{
    [Fact]
    [HappyPath]
    public async Task Heartbeat_endpoint_should_return_a_running_message()
    {
        // When the heartbeat endpoint is called
        var response = await Client.GetAsync($"/{Endpoints.Heartbeat}");

        // Then the response should indicate the service is running
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = Json.Deserialize<TestHeartbeatResponse>(content);
        result.Should().NotBeNull();
        result!.Status.Should().Be(Documentation.HeartbeatStatus);
        this.BDDfy();
    }

    private record TestHeartbeatResponse(string Status);
}
