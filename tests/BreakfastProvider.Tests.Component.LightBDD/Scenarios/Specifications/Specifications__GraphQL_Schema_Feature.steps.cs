using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Specifications__GraphQL_Schema_Feature : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications__GraphQL_Schema_Feature() => _documentSteps = Get<SpecificationDocumentSteps>();

    #region Given
    #endregion

    #region When

    private async Task The_graphql_schema_endpoint_is_called()
        => await _documentSteps.Retrieve(Endpoints.GraphQLContract.SchemaJson);

    private async Task The_graphql_schema_definition_endpoint_is_called()
        => await _documentSteps.Retrieve(Endpoints.GraphQLContract.SchemaDefinition);

    #endregion

    #region Then

    private async Task<CompositeStep> The_response_should_be_valid()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_should_be_valid_json());
    }

    private async Task The_response_status_should_be_ok()
        => _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_response_should_be_valid_json()
        => _documentSteps.Json.Should().NotBeNull();

    private async Task<CompositeStep> The_schema_should_contain_all_the_reporting_queries()
    {
        return Sub.Steps(
            _ => The_query_type_should_be_TYPE(GraphQLSchemaDefaults.QueryTypeName),
            _ => The_query_type_should_offer_the_QUERY_query(GraphQLSchemaDefaults.OrderSummaries),
            _ => The_query_type_should_offer_the_QUERY_query(GraphQLSchemaDefaults.RecipeReports),
            _ => The_query_type_should_offer_the_QUERY_query(GraphQLSchemaDefaults.IngredientUsage),
            _ => The_query_type_should_offer_the_QUERY_query(GraphQLSchemaDefaults.PopularRecipes),
            _ => The_query_type_should_offer_the_QUERY_query(GraphQLSchemaDefaults.BatchCompletions),
            _ => The_query_type_should_offer_the_QUERY_query(GraphQLSchemaDefaults.IngredientShipments),
            _ => The_query_type_should_offer_the_QUERY_query(GraphQLSchemaDefaults.EquipmentAlerts));
    }

    private async Task The_query_type_should_be_TYPE(string type)
        => GraphQLSchemaDocuments.QueryTypeName(_documentSteps.Json!).Should().Be(type);

    private async Task The_query_type_should_offer_the_QUERY_query(string query)
        => GraphQLSchemaDocuments.FieldNames(_documentSteps.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().Contain(query);

    private async Task<CompositeStep> The_reporting_queries_should_be_documented()
    {
        return Sub.Steps(
            _ => The_query_type_should_carry_a_description(),
            _ => Every_query_should_carry_a_description());
    }

    private async Task The_query_type_should_carry_a_description()
        => GraphQLSchemaDocuments.TypeDescription(_documentSteps.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().NotBeNullOrWhiteSpace();

    private async Task Every_query_should_carry_a_description()
        => GraphQLSchemaDocuments.UndocumentedFields(_documentSteps.Json!, GraphQLSchemaDefaults.QueryTypeName).Should().BeEmpty();

    private async Task The_graphql_schema_is_written_to_disk()
    {
        var path = await _documentSteps.WriteToDocs(GraphQLSpecs.JsonFileName);
        await StepExecution.Current.AttachFile(m => m.CreateFromFile(GraphQLSpecs.JsonFileName, path, removeOriginalFile: false));
    }

    private async Task<CompositeStep> The_response_should_be_a_graphql_schema_definition()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_content_type_should_be_CONTENT_TYPE(ContentTypes.GraphQL));
    }

    private async Task The_response_content_type_should_be_CONTENT_TYPE(string contentType)
        => _documentSteps.ResponseMessage!.Content.Headers.ContentType!.MediaType.Should().Be(contentType);

    private async Task<CompositeStep> The_schema_definition_should_declare_all_the_reporting_queries()
    {
        return Sub.Steps(
            _ => The_schema_definition_should_declare_the_QUERY_query(GraphQLSchemaDefaults.OrderSummaries),
            _ => The_schema_definition_should_declare_the_QUERY_query(GraphQLSchemaDefaults.RecipeReports),
            _ => The_schema_definition_should_declare_the_QUERY_query(GraphQLSchemaDefaults.IngredientUsage),
            _ => The_schema_definition_should_declare_the_QUERY_query(GraphQLSchemaDefaults.PopularRecipes),
            _ => The_schema_definition_should_declare_the_QUERY_query(GraphQLSchemaDefaults.BatchCompletions),
            _ => The_schema_definition_should_declare_the_QUERY_query(GraphQLSchemaDefaults.IngredientShipments),
            _ => The_schema_definition_should_declare_the_QUERY_query(GraphQLSchemaDefaults.EquipmentAlerts));
    }

    private async Task The_schema_definition_should_declare_the_QUERY_query(string query)
        => GraphQLSchemaDocuments.DeclaresField(_documentSteps.Body!, GraphQLSchemaDefaults.QueryTypeName, query).Should().BeTrue();

    private async Task The_graphql_schema_definition_is_written_to_disk()
    {
        var path = await _documentSteps.WriteToDocs(GraphQLSpecs.SdlFileName);
        await StepExecution.Current.AttachFile(m => m.CreateFromFile(GraphQLSpecs.SdlFileName, path, removeOriginalFile: false));
    }

    #endregion
}
