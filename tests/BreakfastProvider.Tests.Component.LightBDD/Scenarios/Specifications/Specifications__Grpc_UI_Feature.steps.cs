using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Specifications__Grpc_UI_Feature : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications__Grpc_UI_Feature() => _documentSteps = Get<SpecificationDocumentSteps>();

    #region Given
    #endregion

    #region When

    private async Task The_grpc_ui_endpoint_is_called()
        => await _documentSteps.Retrieve(Endpoints.GrpcContract.UI);

    #endregion

    #region Then

    private async Task<CompositeStep> The_response_should_be_a_valid_grpc_documentation_page()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_content_type_should_be_CONTENT_TYPE(MediaTypeNames.Text.Html),
            _ => The_response_should_be_valid_html());
    }

    private async Task The_response_status_should_be_ok()
        => _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_response_content_type_should_be_CONTENT_TYPE(string contentType)
        => _documentSteps.ResponseMessage!.Content.Headers.ContentType!.MediaType.Should().Be(contentType);

    private async Task The_response_should_be_valid_html()
        => _documentSteps.Body.Should().Contain("<html");

    private async Task<CompositeStep> The_page_should_describe_every_breakfast_method()
    {
        return Sub.Steps(
            _ => The_page_should_carry_the_service_description(),
            _ => The_page_should_describe_the_METHOD_method(GrpcContractDefaults.GetRecipeSummary),
            _ => The_page_should_describe_the_METHOD_method(GrpcContractDefaults.GetOrderStatus),
            _ => The_page_should_describe_the_METHOD_method(GrpcContractDefaults.StreamOrderUpdates),
            _ => The_page_should_show_the_METHOD_method_as_KIND(GrpcContractDefaults.StreamOrderUpdates, GrpcContractDefaults.ServerStreamingKind));
    }

    private async Task The_page_should_carry_the_service_description()
        => _documentSteps.Body.Should().Contain(GrpcContractDefaults.ServiceDescription);

    private async Task The_page_should_describe_the_METHOD_method(string method)
        => GrpcContractPageRows.RowOf(_documentSteps.Body!, method).Should().NotBeNull();

    private async Task The_page_should_show_the_METHOD_method_as_KIND(string method, string kind)
        => GrpcContractPageRows.RowOf(_documentSteps.Body!, method).Should().Contain(kind);

    private async Task<CompositeStep> The_page_should_link_to_the_grpc_contract()
    {
        return Sub.Steps(
            _ => The_page_should_link_to_DOCUMENT(GrpcContractDefaults.ContractJsonLink),
            _ => The_page_should_link_to_DOCUMENT(GrpcContractDefaults.ProtoFileLink));
    }

    private async Task The_page_should_link_to_DOCUMENT(string document)
        => _documentSteps.Body.Should().Contain($"href=\"{document}\"");

    private async Task The_grpc_ui_page_is_written_to_disk()
    {
        var path = await _documentSteps.WriteToDocs(GrpcSpecs.HtmlFileName);
        await StepExecution.Current.AttachFile(m => m.CreateFromFile(GrpcSpecs.HtmlFileName, path, removeOriginalFile: false));
    }

    #endregion
}
