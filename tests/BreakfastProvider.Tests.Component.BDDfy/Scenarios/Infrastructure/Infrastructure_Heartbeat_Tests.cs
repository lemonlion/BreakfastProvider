using System.Net;
using BreakfastProvider.Api;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Infrastructure;

public class Infrastructure_Heartbeat_Tests : BaseFixture
{
    private HttpResponseMessage? _response;
    private TestHeartbeatResponse? _result;

    [Fact]
    [HappyPath]
    public void Heartbeat_endpoint_should_return_a_running_message()
    {
        this.When(x => x.The_heartbeat_endpoint_is_called())
            .Then(x => x.The_response_should_indicate_the_service_is_running())
            .BDDfy();
    }

    #region Steps

    private async Task The_heartbeat_endpoint_is_called()
    {
        _response = await Client.GetAsync($"/{Endpoints.Heartbeat}");
        var content = await _response.Content.ReadAsStringAsync();
        _result = Json.Deserialize<TestHeartbeatResponse>(content);
    }

    private void The_response_should_indicate_the_service_is_running()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _result.Should().NotBeNull();
        _result!.Status.Should().Be(Documentation.HeartbeatStatus);
    }

    #endregion

    private record TestHeartbeatResponse(string Status);
}
