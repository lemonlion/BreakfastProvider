using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Specifications__GraphQL_UI_Feature : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications__GraphQL_UI_Feature() => _documentSteps = Get<SpecificationDocumentSteps>();

    #region Given
    #endregion

    #region When

    private async Task The_graphql_ui_endpoint_is_called()
        => await _documentSteps.Retrieve(Endpoints.GraphQLContract.UI, MediaTypeNames.Text.Html);

    #endregion

    #region Then

    private async Task<CompositeStep> The_response_should_be_a_valid_nitro_page()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_should_be_valid_html(),
            _ => The_page_should_be_the_IDE_ide(GraphQLSchemaDefaults.NitroMarker));
    }

    private async Task The_response_status_should_be_ok()
        => _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_response_should_be_valid_html()
        => _documentSteps.Body.Should().Contain("<html");

    private async Task The_page_should_be_the_IDE_ide(string ide)
        => _documentSteps.Body.Should().Contain(ide);

    private async Task The_page_should_be_served_by_the_service_itself()
        => _documentSteps.ResponseMessage!.Headers.Contains(GraphQLSchemaDefaults.CdnRayHeader).Should().BeFalse();

    #endregion
}
