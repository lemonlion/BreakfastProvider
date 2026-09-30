using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;
using Kronikol.Tracking;
using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Specifications;

public class Specifications_Grpc_UI_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_Grpc_UI_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    [Fact]
    [HappyPath]
    [Trait("Produces", "grpc.html")]
    public void The_Grpc_UI_endpoint_should_return_a_page_describing_the_service()
    {
        this.When(x => x.The_grpc_ui_endpoint_is_called())
            .Then(x => x.The_response_should_be_a_valid_grpc_documentation_page())
            .And(x => x.The_page_should_describe_every_breakfast_method())
            .And(x => x.The_page_should_link_to_the_grpc_contract())
            .And(x => x.The_grpc_ui_page_is_written_to_disk())
            .BDDfy();
    }

    #region Steps

    private async Task The_grpc_ui_endpoint_is_called()
    {
        await _documentSteps.Retrieve(Endpoints.GrpcContract.UI);
    }

    private void The_response_should_be_a_valid_grpc_documentation_page()
    {
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Text.Html);
        _documentSteps.Body.Should().Contain("<html");
    }

    private void The_page_should_describe_every_breakfast_method()
    {
        _documentSteps.Body.Should().Contain(GrpcContractDefaults.ServiceDescription);
        GrpcContractPageRows.RowOf(_documentSteps.Body!, GrpcContractDefaults.GetRecipeSummary).Should().NotBeNull();
        GrpcContractPageRows.RowOf(_documentSteps.Body!, GrpcContractDefaults.GetOrderStatus).Should().NotBeNull();
        GrpcContractPageRows.RowOf(_documentSteps.Body!, GrpcContractDefaults.StreamOrderUpdates).Should().Contain(GrpcContractDefaults.ServerStreamingKind);
    }

    private void The_page_should_link_to_the_grpc_contract()
    {
        _documentSteps.Body.Should().Contain($"href=\"{GrpcContractDefaults.ContractJsonLink}\"");
        _documentSteps.Body.Should().Contain($"href=\"{GrpcContractDefaults.ProtoFileLink}\"");
    }

    private async Task The_grpc_ui_page_is_written_to_disk()
    {
        var path = await _documentSteps.WriteToDocs(GrpcSpecs.HtmlFileName);
        Track.Attachment(path, GrpcSpecs.HtmlFileName);
    }

    #endregion
}
