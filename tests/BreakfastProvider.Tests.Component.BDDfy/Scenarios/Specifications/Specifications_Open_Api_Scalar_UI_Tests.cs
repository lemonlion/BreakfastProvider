using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Specifications;

public class Specifications_Open_Api_Scalar_UI_Tests : BaseFixture
{
    private HttpResponseMessage? _scalarResponse;
    private string? _scalarUiResponseBody;

    [Fact]
    [HappyPath]
    public void The_Scalar_UI_endpoint_should_return_a_valid_page()
    {
        this.When(x => x.The_scalar_ui_endpoint_is_called())
            .Then(x => x.The_response_status_should_be_ok())
            .And(x => x.The_response_should_be_valid_html())
            .And(x => x.The_response_should_refer_to_scalar())
            .BDDfy();
    }

    #region Steps

    private async Task The_scalar_ui_endpoint_is_called()
    {
        _scalarResponse = await Client.GetAsync(Endpoints.Swagger.ScalarUI);
        _scalarUiResponseBody = await _scalarResponse.Content.ReadAsStringAsync();
    }

    private void The_response_status_should_be_ok()
    {
        _scalarResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void The_response_should_be_valid_html()
    {
        _scalarUiResponseBody.Should().Contain("<html");
    }

    private void The_response_should_refer_to_scalar()
    {
        _scalarUiResponseBody.Should().Contain("scalar");
    }

    #endregion
}
