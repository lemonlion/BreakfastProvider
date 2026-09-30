using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using Google.Protobuf.Reflection;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Specifications__Grpc_Contract_Feature : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications__Grpc_Contract_Feature() => _documentSteps = Get<SpecificationDocumentSteps>();

    private FileDescriptorProto ContractFile => _documentSteps.DescriptorSet!.File.Single();
    private ServiceDescriptorProto BreakfastService => ContractFile.Service.Single(s => s.Name == GrpcContractDefaults.ServiceName);

    #region Given
    #endregion

    #region When

    private async Task The_grpc_contract_endpoint_is_called()
        => await _documentSteps.Retrieve(Endpoints.GrpcContract.ContractJson);

    private async Task The_grpc_proto_endpoint_is_called()
        => await _documentSteps.Retrieve(Endpoints.GrpcContract.ProtoFile);

    #endregion

    #region Then

    private async Task<CompositeStep> The_response_should_be_valid()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_should_be_a_valid_descriptor_set());
    }

    private async Task The_response_status_should_be_ok()
        => _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_response_should_be_a_valid_descriptor_set()
        => _documentSteps.DescriptorSet.Should().NotBeNull();

    private async Task<CompositeStep> The_grpc_contract_should_describe_the_breakfast_service()
    {
        return Sub.Steps(
            _ => The_contract_should_hold_only_the_FILE_file(GrpcContractDefaults.ProtoFileName),
            _ => The_contract_should_be_in_the_PACKAGE_package(GrpcContractDefaults.Package),
            _ => The_contract_should_declare_the_SERVICE_service(GrpcContractDefaults.ServiceName),
            _ => The_breakfast_service_should_offer_the_METHOD_method(GrpcContractDefaults.GetRecipeSummary),
            _ => The_breakfast_service_should_offer_the_METHOD_method(GrpcContractDefaults.GetOrderStatus),
            _ => The_breakfast_service_should_offer_the_METHOD_method(GrpcContractDefaults.StreamOrderUpdates),
            _ => The_METHOD_method_should_stream_its_replies(GrpcContractDefaults.StreamOrderUpdates));
    }

    private async Task The_contract_should_hold_only_the_FILE_file(string file)
        => _documentSteps.DescriptorSet!.File.Select(f => f.Name).Should().Equal(file);

    private async Task The_contract_should_be_in_the_PACKAGE_package(string package)
        => ContractFile.Package.Should().Be(package);

    private async Task The_contract_should_declare_the_SERVICE_service(string service)
        => ContractFile.Service.Select(s => s.Name).Should().Contain(service);

    private async Task The_breakfast_service_should_offer_the_METHOD_method(string method)
        => BreakfastService.Method.Select(m => m.Name).Should().Contain(method);

    private async Task The_METHOD_method_should_stream_its_replies(string method)
        => BreakfastService.Method.Single(m => m.Name == method).ServerStreaming.Should().BeTrue();

    private async Task Every_breakfast_method_should_be_documented()
        => GrpcDescriptorComments.UndocumentedMethods(ContractFile, GrpcContractDefaults.ServiceName).Should().BeEmpty();

    private async Task The_grpc_contract_is_written_to_disk()
    {
        var path = await _documentSteps.WriteToDocs(GrpcSpecs.JsonFileName);
        await StepExecution.Current.AttachFile(m => m.CreateFromFile(GrpcSpecs.JsonFileName, path, removeOriginalFile: false));
    }

    private async Task<CompositeStep> The_response_should_be_a_plain_text_proto_file()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_content_type_should_be_CONTENT_TYPE(MediaTypeNames.Text.Plain));
    }

    private async Task The_response_content_type_should_be_CONTENT_TYPE(string contentType)
        => _documentSteps.ResponseMessage!.Content.Headers.ContentType!.MediaType.Should().Be(contentType);

    private async Task<CompositeStep> The_proto_file_should_declare_the_breakfast_service()
    {
        return Sub.Steps(
            _ => The_proto_file_should_declare_the_PACKAGE_package(GrpcContractDefaults.Package),
            _ => The_proto_file_should_declare_the_SERVICE_service(GrpcContractDefaults.ServiceName));
    }

    private async Task The_proto_file_should_declare_the_PACKAGE_package(string package)
        => _documentSteps.Body.Should().Contain($"package {package};");

    private async Task The_proto_file_should_declare_the_SERVICE_service(string service)
        => _documentSteps.Body.Should().Contain($"service {service}");

    #endregion
}
