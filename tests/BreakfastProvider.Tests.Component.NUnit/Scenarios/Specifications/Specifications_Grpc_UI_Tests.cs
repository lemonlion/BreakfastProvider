using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.NUnit.Infrastructure;
using Kronikol.Tracking;
using Kronikol.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.Specifications;

public class Specifications_Grpc_UI_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_Grpc_UI_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    [Test]
    [HappyPath]
    [Category("Produces: grpc.html")]
    public async Task The_Grpc_UI_endpoint_should_return_a_page_describing_the_service()
    {
        // When the grpc ui endpoint is called
        await _documentSteps.Retrieve(Endpoints.GrpcContract.UI);

        // Then the response should be a valid grpc documentation page
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Text.Html);
        _documentSteps.Body.Should().Contain("<html");

        // And the page should describe every breakfast method
        _documentSteps.Body.Should().Contain(GrpcContractDefaults.ServiceDescription);
        GrpcContractPageRows.RowOf(_documentSteps.Body!, GrpcContractDefaults.GetRecipeSummary).Should().NotBeNull();
        GrpcContractPageRows.RowOf(_documentSteps.Body!, GrpcContractDefaults.GetOrderStatus).Should().NotBeNull();
        GrpcContractPageRows.RowOf(_documentSteps.Body!, GrpcContractDefaults.StreamOrderUpdates).Should().Contain(GrpcContractDefaults.ServerStreamingKind);

        // And the page should link to the grpc contract
        _documentSteps.Body.Should().Contain($"href=\"{GrpcContractDefaults.ContractJsonLink}\"");
        _documentSteps.Body.Should().Contain($"href=\"{GrpcContractDefaults.ProtoFileLink}\"");

        // And the grpc ui page is written to disk
        var path = await _documentSteps.WriteToDocs(GrpcSpecs.HtmlFileName);
        Track.Attachment(path, GrpcSpecs.HtmlFileName);
    }
}
