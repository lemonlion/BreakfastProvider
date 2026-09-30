using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using Google.Protobuf.Reflection;
using Kronikol.Tracking;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Specifications;

public class Specifications_Grpc_Contract_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_Grpc_Contract_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    private FileDescriptorProto ContractFile => _documentSteps.DescriptorSet!.File.Single();
    private ServiceDescriptorProto BreakfastService => ContractFile.Service.Single(s => s.Name == GrpcContractDefaults.ServiceName);

    [Test]
    [HappyPath]
    [Property("Produces", "grpc.json")]
    public async Task The_Grpc_contract_endpoint_should_return_a_valid_specification()
    {
        // When the grpc contract endpoint is called
        await _documentSteps.Retrieve(Endpoints.GrpcContract.ContractJson);

        // Then the response should be valid
        await _documentSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _documentSteps.DescriptorSet.Should().NotBeNull();

        // And the grpc contract should describe the breakfast service
        await _documentSteps.DescriptorSet!.File.Should().HaveCount(1);
        await ContractFile.Name.Should().BeEqualTo(GrpcContractDefaults.ProtoFileName);
        await ContractFile.Package.Should().BeEqualTo(GrpcContractDefaults.Package);
        await ContractFile.Service.Should().Contain(s => s.Name == GrpcContractDefaults.ServiceName);
        await BreakfastService.Method.Should().Contain(m => m.Name == GrpcContractDefaults.GetRecipeSummary);
        await BreakfastService.Method.Should().Contain(m => m.Name == GrpcContractDefaults.GetOrderStatus);
        await BreakfastService.Method.Should().Contain(m => m.Name == GrpcContractDefaults.StreamOrderUpdates);
        await BreakfastService.Method.Single(m => m.Name == GrpcContractDefaults.StreamOrderUpdates).ServerStreaming.Should().BeTrue();

        // And every breakfast method should be documented
        await GrpcDescriptorComments.UndocumentedMethods(ContractFile, GrpcContractDefaults.ServiceName).Should().BeEmpty();

        // And the grpc contract is written to disk
        var path = await _documentSteps.WriteToDocs(GrpcSpecs.JsonFileName);
        Track.Attachment(path, GrpcSpecs.JsonFileName);
    }

    [Test]
    [HappyPath]
    public async Task The_Grpc_proto_endpoint_should_return_the_proto_file()
    {
        // When the grpc proto endpoint is called
        await _documentSteps.Retrieve(Endpoints.GrpcContract.ProtoFile);

        // Then the response should be a plain text proto file
        await _documentSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _documentSteps.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().BeEqualTo(MediaTypeNames.Text.Plain);

        // And the proto file should declare the breakfast service
        await _documentSteps.Body!.Should().Contain($"package {GrpcContractDefaults.Package};");
        await _documentSteps.Body!.Should().Contain($"service {GrpcContractDefaults.ServiceName}");
    }
}
