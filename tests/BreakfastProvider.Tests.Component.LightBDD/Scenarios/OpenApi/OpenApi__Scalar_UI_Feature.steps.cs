using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.OpenApi;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class OpenApi__Scalar_UI_Feature : BaseFixture
{
    private HttpResponseMessage? _scalarResponse;
    private string? _scalarHtml;

    #region Given
    #endregion

    #region When

    private async Task The_scalar_ui_endpoint_is_called()
    {
        _scalarResponse = await Client.GetAsync(Endpoints.Swagger.ScalarUI);
    }

    #endregion

    #region Then

    private async Task<CompositeStep> The_response_should_be_a_valid_scalar_page()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_should_be_valid_html(),
            _ => The_response_should_refer_to_scalar());
    }

    private async Task The_response_status_should_be_ok()
    {
        Track.That(() => _scalarResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    private async Task The_response_should_be_valid_html()
    {
        _scalarHtml = await _scalarResponse!.Content.ReadAsStringAsync();
        var scalarUiResponseBody = _scalarHtml;
        Track.That(() => scalarUiResponseBody.Should().Contain("<html"));
    }

    private async Task The_response_should_refer_to_scalar()
    {
        var scalarUiResponseBody = _scalarHtml;
        Track.That(() => scalarUiResponseBody.Should().Contain("scalar"));
    }

    #endregion
}
