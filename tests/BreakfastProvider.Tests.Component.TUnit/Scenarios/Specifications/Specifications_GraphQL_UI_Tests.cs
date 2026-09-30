using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Specifications;

public class Specifications_GraphQL_UI_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_GraphQL_UI_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    [Test]
    [HappyPath]
    public async Task The_GraphQL_UI_endpoint_should_return_a_valid_page()
    {
        // When the graphql ui endpoint is called
        await _documentSteps.Retrieve(Endpoints.GraphQLContract.UI, MediaTypeNames.Text.Html);

        // Then the response should be a valid nitro page
        await _documentSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _documentSteps.Body!.Should().Contain("<html");
        await _documentSteps.Body!.Should().Contain(GraphQLSchemaDefaults.NitroMarker);

        // And the page should be served by the service itself
        await _documentSteps.ResponseMessage.Headers.Contains(GraphQLSchemaDefaults.CdnRayHeader).Should().BeFalse();
    }
}
