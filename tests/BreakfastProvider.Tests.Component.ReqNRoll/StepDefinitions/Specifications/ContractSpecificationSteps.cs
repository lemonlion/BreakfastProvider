using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using Google.Protobuf.Reflection;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Specifications;

/// <summary>
/// The contract pages and documents beyond OpenAPI: the AsyncAPI UI and the gRPC contract. What a When fetches goes
/// into the scenario's <see cref="SpecificationDocumentContext"/>, which the shared "the response should be valid"
/// checks.
/// </summary>
[Binding]
public class ContractSpecificationSteps(SpecificationDocumentContext context, IReqnrollOutputHelper outputHelper)
{
    private FileDescriptorProto ContractFile => context.Document.DescriptorSet!.File.Single();
    private ServiceDescriptorProto BreakfastService => ContractFile.Service.Single(s => s.Name == GrpcContractDefaults.ServiceName);

    // ── AsyncAPI UI ──

    [When("the asyncapi ui endpoint is called")]
    public async Task WhenTheAsyncapiUiEndpointIsCalled()
    {
        await context.Document.Retrieve(Endpoints.AsyncApi.AsyncApiUI);
    }

    [Then("the response should be a valid asyncapi page")]
    public void ThenTheResponseShouldBeAValidAsyncapiPage()
    {
        context.Document.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        context.Document.Body.Should().Contain("<html");
        context.Document.Body.Should().Contain(AsyncApiSpecs.UiRenderer);
        context.Document.Body.Should().Contain(Endpoints.AsyncApi.AsyncApiSpec);
    }

    // ── gRPC contract ──

    [When("the grpc contract endpoint is called")]
    public async Task WhenTheGrpcContractEndpointIsCalled()
    {
        await context.Retrieve(Endpoints.GrpcContract.ContractJson, SpecificationDocumentFormat.DescriptorSet);
    }

    [Then("the grpc contract should describe the breakfast service")]
    public void ThenTheGrpcContractShouldDescribeTheBreakfastService()
    {
        context.Document.DescriptorSet!.File.Select(f => f.Name).Should().Equal(GrpcContractDefaults.ProtoFileName);
        ContractFile.Package.Should().Be(GrpcContractDefaults.Package);
        ContractFile.Service.Select(s => s.Name).Should().Contain(GrpcContractDefaults.ServiceName);
        BreakfastService.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetRecipeSummary);
        BreakfastService.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetOrderStatus);
        BreakfastService.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.StreamOrderUpdates);
        BreakfastService.Method.Single(m => m.Name == GrpcContractDefaults.StreamOrderUpdates).ServerStreaming.Should().BeTrue();
    }

    [Then("every breakfast method should be documented")]
    public void ThenEveryBreakfastMethodShouldBeDocumented()
    {
        GrpcDescriptorComments.UndocumentedMethods(ContractFile, GrpcContractDefaults.ServiceName).Should().BeEmpty();
    }

    [Then("the grpc contract is written to disk")]
    public async Task ThenTheGrpcContractIsWrittenToDisk()
    {
        var path = await context.Document.WriteToDocs(GrpcSpecs.JsonFileName);
        outputHelper.AddAttachment(path);
    }

    [When("the grpc proto endpoint is called")]
    public async Task WhenTheGrpcProtoEndpointIsCalled()
    {
        await context.Document.Retrieve(Endpoints.GrpcContract.ProtoFile);
    }

    [Then("the response should be a plain text proto file")]
    public void ThenTheResponseShouldBeAPlainTextProtoFile()
    {
        context.Document.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        context.Document.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Text.Plain);
    }

    [Then("the proto file should declare the breakfast service")]
    public void ThenTheProtoFileShouldDeclareTheBreakfastService()
    {
        context.Document.Body.Should().Contain($"package {GrpcContractDefaults.Package};");
        context.Document.Body.Should().Contain($"service {GrpcContractDefaults.ServiceName}");
    }
}
