using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using Kronikol.Tracking;
using Kronikol.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Specifications;

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
    public async Task The_GraphQL_schema_endpoint_should_return_a_valid_specification()
    {
        // When the graphql schema endpoint is called
        await _documentSteps.Retrieve(Endpoints.GraphQLContract.SchemaJson);

        // Then the response should be valid
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.Json.Should().NotBeNull();

        // And the schema should contain all the reporting queries
        GraphQLSchemaDocuments.QueryTypeName(_documentSteps.Json!).Should().Be(GraphQLSchemaDefaults.QueryTypeName);
        foreach (var query in ReportingQueries)
            GraphQLSchemaDocuments.FieldNames(_documentSteps.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().Contain(query);

        // And the reporting queries should be documented
        GraphQLSchemaDocuments.TypeDescription(_documentSteps.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().NotBeNullOrWhiteSpace();
        GraphQLSchemaDocuments.UndocumentedFields(_documentSteps.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().BeEmpty();

        // And the graphql schema is written to disk
        var path = await _documentSteps.WriteToDocs(GraphQLSpecs.JsonFileName);
        Track.Attachment(path, GraphQLSpecs.JsonFileName);
    }

    [Fact]
    [HappyPath]
    [Trait("Produces", "schema.graphql")]
    public async Task The_GraphQL_schema_definition_endpoint_should_return_the_schema_definition()
    {
        // When the graphql schema definition endpoint is called
        await _documentSteps.Retrieve(Endpoints.GraphQLContract.SchemaDefinition);

        // Then the response should be a graphql schema definition
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.ResponseMessage.Content.Headers.ContentType!.MediaType.Should().Be(ContentTypes.GraphQL);

        // And the schema definition should declare all the reporting queries
        foreach (var query in ReportingQueries)
            GraphQLSchemaDocuments.DeclaresField(_documentSteps.Body!, GraphQLSchemaDefaults.QueryTypeName, query).Should().BeTrue(query);

        // And the graphql schema definition is written to disk
        var path = await _documentSteps.WriteToDocs(GraphQLSpecs.SdlFileName);
        Track.Attachment(path, GraphQLSpecs.SdlFileName);
    }
}
