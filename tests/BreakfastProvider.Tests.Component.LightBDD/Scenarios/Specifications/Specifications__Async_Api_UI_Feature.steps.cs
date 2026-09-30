using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Specifications__Async_Api_UI_Feature : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications__Async_Api_UI_Feature() => _documentSteps = Get<SpecificationDocumentSteps>();

    #region Given
    #endregion

    #region When

    private async Task The_asyncapi_ui_endpoint_is_called()
        => await _documentSteps.Retrieve(Endpoints.AsyncApi.AsyncApiUI);

    #endregion

    #region Then

    private async Task<CompositeStep> The_response_should_be_a_valid_asyncapi_page()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_should_be_valid_html(),
            _ => The_page_should_render_the_document_with_RENDERER(AsyncApiSpecs.UiRenderer),
            _ => The_page_should_render_the_document_at_PATH(Endpoints.AsyncApi.AsyncApiSpec));
    }

    private async Task The_response_status_should_be_ok()
        => _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_response_should_be_valid_html()
        => _documentSteps.Body.Should().Contain("<html");

    private async Task The_page_should_render_the_document_with_RENDERER(string renderer)
        => _documentSteps.Body.Should().Contain(renderer);

    private async Task The_page_should_render_the_document_at_PATH(string path)
        => _documentSteps.Body.Should().Contain(path);

    #endregion
}
