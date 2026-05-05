using System.Net;
using BreakfastProvider.Api;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Infrastructure;

#pragma warning disable CS1998
public partial class Infrastructure__Heartbeat_Feature : BaseFixture
{
    private HttpResponseMessage? _heartbeatResponse;
    private TestHeartbeatResponse? _heartbeatResult;

    #region Given
    #endregion

    #region When

    private async Task The_heartbeat_endpoint_is_called()
    {
        _heartbeatResponse = await Client.GetAsync($"/{Endpoints.Heartbeat}");
    }

    #endregion

    #region Then

    private async Task<CompositeStep> The_heartbeat_response_should_indicate_the_service_is_running()
    {
        return Sub.Steps(
            _ => The_heartbeat_response_status_should_be_ok(),
            _ => The_heartbeat_response_should_be_valid_json(),
            _ => The_heartbeat_status_should_be_ok());
    }

    private async Task The_heartbeat_response_status_should_be_ok()
        => _heartbeatResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_heartbeat_response_should_be_valid_json()
    {
        var content = await _heartbeatResponse!.Content.ReadAsStringAsync();
        _heartbeatResult = Json.Deserialize<TestHeartbeatResponse>(content);
        _heartbeatResult.Should().NotBeNull();
    }

    private async Task The_heartbeat_status_should_be_ok()
        => _heartbeatResult!.Status.Should().Be(Documentation.HeartbeatStatus);

    #endregion

    private record TestHeartbeatResponse(string Status);
}
