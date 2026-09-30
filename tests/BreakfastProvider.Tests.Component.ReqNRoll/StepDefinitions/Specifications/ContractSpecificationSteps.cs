using System.Net;
using System.Net.Mime;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using Google.Protobuf.Reflection;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Specifications;

/// <summary>
/// The contract pages and documents beyond OpenAPI: the AsyncAPI UI, the gRPC contract and its page, and the GraphQL
/// schema and its IDE. What a When fetches goes
/// into the scenario's <see cref="SpecificationDocumentContext"/>, which the shared "the response should be valid"
/// checks.
/// </summary>
[Binding]
public class ContractSpecificationSteps(SpecificationDocumentContext context, IReqnrollOutputHelper outputHelper)
{
    private static readonly string[] ReportingQueries =
    [
        GraphQLSchemaDefaults.OrderSummaries, GraphQLSchemaDefaults.RecipeReports, GraphQLSchemaDefaults.IngredientUsage,
        GraphQLSchemaDefaults.PopularRecipes, GraphQLSchemaDefaults.BatchCompletions,
        GraphQLSchemaDefaults.IngredientShipments, GraphQLSchemaDefaults.EquipmentAlerts
    ];

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

    // ── gRPC UI ──

    [When("the grpc ui endpoint is called")]
    public async Task WhenTheGrpcUiEndpointIsCalled()
    {
        await context.Document.Retrieve(Endpoints.GrpcContract.UI);
    }

    [Then("the response should be a valid grpc documentation page")]
    public void ThenTheResponseShouldBeAValidGrpcDocumentationPage()
    {
        context.Document.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        context.Document.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Text.Html);
        context.Document.Body.Should().Contain("<html");
    }

    [Then("the page should describe every breakfast method")]
    public void ThenThePageShouldDescribeEveryBreakfastMethod()
    {
        context.Document.Body.Should().Contain(GrpcContractDefaults.ServiceDescription);
        GrpcContractPageRows.RowOf(context.Document.Body!, GrpcContractDefaults.GetRecipeSummary).Should().NotBeNull();
        GrpcContractPageRows.RowOf(context.Document.Body!, GrpcContractDefaults.GetOrderStatus).Should().NotBeNull();
        GrpcContractPageRows.RowOf(context.Document.Body!, GrpcContractDefaults.StreamOrderUpdates).Should().Contain(GrpcContractDefaults.ServerStreamingKind);
    }

    [Then("the page should link to the grpc contract")]
    public void ThenThePageShouldLinkToTheGrpcContract()
    {
        context.Document.Body.Should().Contain($"href=\"{GrpcContractDefaults.ContractJsonLink}\"");
        context.Document.Body.Should().Contain($"href=\"{GrpcContractDefaults.ProtoFileLink}\"");
    }

    [Then("the grpc ui page is written to disk")]
    public async Task ThenTheGrpcUiPageIsWrittenToDisk()
    {
        var path = await context.Document.WriteToDocs(GrpcSpecs.HtmlFileName);
        outputHelper.AddAttachment(path);
    }

    // ── GraphQL schema ──

    [When("the graphql schema endpoint is called")]
    public async Task WhenTheGraphqlSchemaEndpointIsCalled()
    {
        await context.Retrieve(Endpoints.GraphQLContract.SchemaJson, SpecificationDocumentFormat.Json);
    }

    [Then("the schema should contain all the reporting queries")]
    public void ThenTheSchemaShouldContainAllTheReportingQueries()
    {
        GraphQLSchemaDocuments.QueryTypeName(context.Document.Json!).Should().Be(GraphQLSchemaDefaults.QueryTypeName);
        foreach (var query in ReportingQueries)
            GraphQLSchemaDocuments.FieldNames(context.Document.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().Contain(query);
    }

    [Then("the reporting queries should be documented")]
    public void ThenTheReportingQueriesShouldBeDocumented()
    {
        GraphQLSchemaDocuments.TypeDescription(context.Document.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().NotBeNullOrWhiteSpace();
        GraphQLSchemaDocuments.UndocumentedFields(context.Document.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().BeEmpty();
    }

    [Then("the graphql schema is written to disk")]
    public async Task ThenTheGraphqlSchemaIsWrittenToDisk()
    {
        var path = await context.Document.WriteToDocs(GraphQLSpecs.JsonFileName);
        outputHelper.AddAttachment(path);
    }

    [When("the graphql schema definition endpoint is called")]
    public async Task WhenTheGraphqlSchemaDefinitionEndpointIsCalled()
    {
        await context.Document.Retrieve(Endpoints.GraphQLContract.SchemaDefinition);
    }

    [Then("the response should be a graphql schema definition")]
    public void ThenTheResponseShouldBeAGraphqlSchemaDefinition()
    {
        context.Document.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        context.Document.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().Be(ContentTypes.GraphQL);
    }

    [Then("the schema definition should declare all the reporting queries")]
    public void ThenTheSchemaDefinitionShouldDeclareAllTheReportingQueries()
    {
        foreach (var query in ReportingQueries)
            GraphQLSchemaDocuments.DeclaresField(context.Document.Body!, GraphQLSchemaDefaults.QueryTypeName, query).Should().BeTrue(query);
    }

    [Then("the graphql schema definition is written to disk")]
    public async Task ThenTheGraphqlSchemaDefinitionIsWrittenToDisk()
    {
        var path = await context.Document.WriteToDocs(GraphQLSpecs.SdlFileName);
        outputHelper.AddAttachment(path);
    }

    // ── GraphQL UI ──

    [When("the graphql ui endpoint is called")]
    public async Task WhenTheGraphqlUiEndpointIsCalled()
    {
        await context.Document.Retrieve(Endpoints.GraphQLContract.UI, MediaTypeNames.Text.Html);
    }

    [Then("the response should be a valid nitro page")]
    public void ThenTheResponseShouldBeAValidNitroPage()
    {
        context.Document.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        context.Document.Body.Should().Contain("<html");
        context.Document.Body.Should().Contain(GraphQLSchemaDefaults.NitroMarker);
    }

    [Then("the page should be served by the service itself")]
    public void ThenThePageShouldBeServedByTheServiceItself()
    {
        context.Document.ResponseMessage!.Headers.Contains(GraphQLSchemaDefaults.CdnRayHeader).Should().BeFalse();
    }
}
