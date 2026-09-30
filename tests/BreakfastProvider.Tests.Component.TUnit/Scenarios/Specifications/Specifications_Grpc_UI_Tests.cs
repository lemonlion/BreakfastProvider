using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using Kronikol.Tracking;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Specifications;

public class Specifications_Grpc_UI_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_Grpc_UI_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    [Test]
    [HappyPath]
    [Property("Produces", "grpc.html")]
    public async Task The_Grpc_UI_endpoint_should_return_a_page_describing_the_service()
    {
        // When the grpc ui endpoint is called
        await _documentSteps.Retrieve(Endpoints.GrpcContract.UI);

        // Then the response should be a valid grpc documentation page
        await _documentSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _documentSteps.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().BeEqualTo(MediaTypeNames.Text.Html);
        await _documentSteps.Body!.Should().Contain("<html");

        // And the page should describe every breakfast method
        await _documentSteps.Body!.Should().Contain(GrpcContractDefaults.ServiceDescription);
        await GrpcContractPageRows.RowOf(_documentSteps.Body!, GrpcContractDefaults.GetRecipeSummary).Should().NotBeNull();
        await GrpcContractPageRows.RowOf(_documentSteps.Body!, GrpcContractDefaults.GetOrderStatus).Should().NotBeNull();
        await GrpcContractPageRows.RowOf(_documentSteps.Body!, GrpcContractDefaults.StreamOrderUpdates)!.Should().Contain(GrpcContractDefaults.ServerStreamingKind);

        // And the page should link to the grpc contract
        await _documentSteps.Body!.Should().Contain($"href=\"{GrpcContractDefaults.ContractJsonLink}\"");
        await _documentSteps.Body!.Should().Contain($"href=\"{GrpcContractDefaults.ProtoFileLink}\"");

        // And the grpc ui page is written to disk
        var path = await _documentSteps.WriteToDocs(GrpcSpecs.HtmlFileName);
        Track.Attachment(path, GrpcSpecs.HtmlFileName);
    }
}
