using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;
using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Specifications;

public class Specifications_GraphQL_UI_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_GraphQL_UI_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    [Fact]
    [HappyPath]
    public void The_GraphQL_UI_endpoint_should_return_a_valid_page()
    {
        this.When(x => x.The_graphql_ui_endpoint_is_called())
            .Then(x => x.The_response_should_be_a_valid_nitro_page())
            .And(x => x.The_page_should_be_served_by_the_service_itself())
            .BDDfy();
    }

    #region Steps

    private async Task The_graphql_ui_endpoint_is_called()
    {
        await _documentSteps.Retrieve(Endpoints.GraphQLContract.UI, MediaTypeNames.Text.Html);
    }

    private void The_response_should_be_a_valid_nitro_page()
    {
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.Body.Should().Contain("<html");
        _documentSteps.Body.Should().Contain(GraphQLSchemaDefaults.NitroMarker);
    }

    private void The_page_should_be_served_by_the_service_itself()
    {
        _documentSteps.ResponseMessage!.Headers.Contains(GraphQLSchemaDefaults.CdnRayHeader).Should().BeFalse();
    }

    #endregion
}
