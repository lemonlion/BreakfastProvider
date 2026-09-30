using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;
using Kronikol.Tracking;
using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Specifications;

public class Specifications_GraphQL_Schema_Tests : BaseFixture
{
    private static readonly string[] ReportingQueries =
    [
        GraphQLSchemaDefaults.OrderSummaries, GraphQLSchemaDefaults.RecipeReports, GraphQLSchemaDefaults.IngredientUsage,
        GraphQLSchemaDefaults.PopularRecipes, GraphQLSchemaDefaults.BatchCompletions,
        GraphQLSchemaDefaults.IngredientShipments, GraphQLSchemaDefaults.EquipmentAlerts
    ];

    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_GraphQL_Schema_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    [Fact]
    [HappyPath]
    [Trait("Produces", "graphql.json")]
    public void The_GraphQL_schema_endpoint_should_return_a_valid_specification()
    {
        this.When(x => x.The_graphql_schema_endpoint_is_called())
            .Then(x => x.The_response_should_be_valid())
            .And(x => x.The_schema_should_contain_all_the_reporting_queries())
            .And(x => x.The_reporting_queries_should_be_documented())
            .And(x => x.The_graphql_schema_is_written_to_disk())
            .BDDfy();
    }

    [Fact]
    [HappyPath]
    [Trait("Produces", "schema.graphql")]
    public void The_GraphQL_schema_definition_endpoint_should_return_the_schema_definition()
    {
        this.When(x => x.The_graphql_schema_definition_endpoint_is_called())
            .Then(x => x.The_response_should_be_a_graphql_schema_definition())
            .And(x => x.The_schema_definition_should_declare_all_the_reporting_queries())
            .And(x => x.The_graphql_schema_definition_is_written_to_disk())
            .BDDfy();
    }

    #region Steps

    private async Task The_graphql_schema_endpoint_is_called()
    {
        await _documentSteps.Retrieve(Endpoints.GraphQLContract.SchemaJson);
    }

    private void The_response_should_be_valid()
    {
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.Json.Should().NotBeNull();
    }

    private void The_schema_should_contain_all_the_reporting_queries()
    {
        GraphQLSchemaDocuments.QueryTypeName(_documentSteps.Json!).Should().Be(GraphQLSchemaDefaults.QueryTypeName);
        foreach (var query in ReportingQueries)
            GraphQLSchemaDocuments.FieldNames(_documentSteps.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().Contain(query);
    }

    private void The_reporting_queries_should_be_documented()
    {
        GraphQLSchemaDocuments.TypeDescription(_documentSteps.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().NotBeNullOrWhiteSpace();
        GraphQLSchemaDocuments.UndocumentedFields(_documentSteps.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().BeEmpty();
    }

    private async Task The_graphql_schema_is_written_to_disk()
    {
        var path = await _documentSteps.WriteToDocs(GraphQLSpecs.JsonFileName);
        Track.Attachment(path, GraphQLSpecs.JsonFileName);
    }

    private async Task The_graphql_schema_definition_endpoint_is_called()
    {
        await _documentSteps.Retrieve(Endpoints.GraphQLContract.SchemaDefinition);
    }

    private void The_response_should_be_a_graphql_schema_definition()
    {
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().Be(ContentTypes.GraphQL);
    }

    private void The_schema_definition_should_declare_all_the_reporting_queries()
    {
        foreach (var query in ReportingQueries)
            GraphQLSchemaDocuments.DeclaresField(_documentSteps.Body!, GraphQLSchemaDefaults.QueryTypeName, query).Should().BeTrue(query);
    }

    private async Task The_graphql_schema_definition_is_written_to_disk()
    {
        var path = await _documentSteps.WriteToDocs(GraphQLSpecs.SdlFileName);
        Track.Attachment(path, GraphQLSpecs.SdlFileName);
    }

    #endregion
}
