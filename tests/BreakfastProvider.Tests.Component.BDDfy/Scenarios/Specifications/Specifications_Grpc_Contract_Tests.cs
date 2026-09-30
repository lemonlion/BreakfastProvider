using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;
using Google.Protobuf.Reflection;
using Kronikol.Tracking;
using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Specifications;

public class Specifications_Grpc_Contract_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_Grpc_Contract_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    private FileDescriptorProto ContractFile => _documentSteps.DescriptorSet!.File.Single();
    private ServiceDescriptorProto BreakfastService => ContractFile.Service.Single(s => s.Name == GrpcContractDefaults.ServiceName);

    [Fact]
    [HappyPath]
    [Trait("Produces", "grpc.json")]
    public void The_Grpc_contract_endpoint_should_return_a_valid_specification()
    {
        this.When(x => x.The_grpc_contract_endpoint_is_called())
            .Then(x => x.The_response_should_be_valid())
            .And(x => x.The_grpc_contract_should_describe_the_breakfast_service())
            .And(x => x.Every_breakfast_method_should_be_documented())
            .And(x => x.The_grpc_contract_is_written_to_disk())
            .BDDfy();
    }

    [Fact]
    [HappyPath]
    public void The_Grpc_proto_endpoint_should_return_the_proto_file()
    {
        this.When(x => x.The_grpc_proto_endpoint_is_called())
            .Then(x => x.The_response_should_be_a_plain_text_proto_file())
            .And(x => x.The_proto_file_should_declare_the_breakfast_service())
            .BDDfy();
    }

    #region Steps

    private async Task The_grpc_contract_endpoint_is_called()
    {
        await _documentSteps.Retrieve(Endpoints.GrpcContract.ContractJson);
    }

    private void The_response_should_be_valid()
    {
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.DescriptorSet.Should().NotBeNull();
    }

    private void The_grpc_contract_should_describe_the_breakfast_service()
    {
        _documentSteps.DescriptorSet!.File.Select(f => f.Name).Should().Equal(GrpcContractDefaults.ProtoFileName);
        ContractFile.Package.Should().Be(GrpcContractDefaults.Package);
        ContractFile.Service.Select(s => s.Name).Should().Contain(GrpcContractDefaults.ServiceName);
        BreakfastService.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetRecipeSummary);
        BreakfastService.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetOrderStatus);
        BreakfastService.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.StreamOrderUpdates);
        BreakfastService.Method.Single(m => m.Name == GrpcContractDefaults.StreamOrderUpdates).ServerStreaming.Should().BeTrue();
    }

    private void Every_breakfast_method_should_be_documented()
    {
        GrpcDescriptorComments.UndocumentedMethods(ContractFile, GrpcContractDefaults.ServiceName).Should().BeEmpty();
    }

    private async Task The_grpc_contract_is_written_to_disk()
    {
        var path = await _documentSteps.WriteToDocs(GrpcSpecs.JsonFileName);
        Track.Attachment(path, GrpcSpecs.JsonFileName);
    }

    private async Task The_grpc_proto_endpoint_is_called()
    {
        await _documentSteps.Retrieve(Endpoints.GrpcContract.ProtoFile);
    }

    private void The_response_should_be_a_plain_text_proto_file()
    {
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Text.Plain);
    }

    private void The_proto_file_should_declare_the_breakfast_service()
    {
        _documentSteps.Body.Should().Contain($"package {GrpcContractDefaults.Package};");
        _documentSteps.Body.Should().Contain($"service {GrpcContractDefaults.ServiceName}");
    }

    #endregion
}
