using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.NUnit.Infrastructure;
using Google.Protobuf.Reflection;
using Kronikol.Tracking;
using Kronikol.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.Specifications;

public class Specifications_Grpc_Contract_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_Grpc_Contract_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    private FileDescriptorProto ContractFile => _documentSteps.DescriptorSet!.File.Single();
    private ServiceDescriptorProto BreakfastService => ContractFile.Service.Single(s => s.Name == GrpcContractDefaults.ServiceName);

    [Test]
    [HappyPath]
    [Category("Produces: grpc.json")]
    public async Task The_Grpc_contract_endpoint_should_return_a_valid_specification()
    {
        // When the grpc contract endpoint is called
        await _documentSteps.Retrieve(Endpoints.GrpcContract.ContractJson);

        // Then the response should be valid
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.DescriptorSet.Should().NotBeNull();

        // And the grpc contract should describe the breakfast service
        _documentSteps.DescriptorSet!.File.Select(f => f.Name).Should().Equal(GrpcContractDefaults.ProtoFileName);
        ContractFile.Package.Should().Be(GrpcContractDefaults.Package);
        ContractFile.Service.Select(s => s.Name).Should().Contain(GrpcContractDefaults.ServiceName);
        BreakfastService.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetRecipeSummary);
        BreakfastService.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetOrderStatus);
        BreakfastService.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.StreamOrderUpdates);
        BreakfastService.Method.Single(m => m.Name == GrpcContractDefaults.StreamOrderUpdates).ServerStreaming.Should().BeTrue();

        // And every breakfast method should be documented
        GrpcDescriptorComments.UndocumentedMethods(ContractFile, GrpcContractDefaults.ServiceName).Should().BeEmpty();

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
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Text.Plain);

        // And the proto file should declare the breakfast service
        _documentSteps.Body.Should().Contain($"package {GrpcContractDefaults.Package};");
        _documentSteps.Body.Should().Contain($"service {GrpcContractDefaults.ServiceName}");
    }
}
