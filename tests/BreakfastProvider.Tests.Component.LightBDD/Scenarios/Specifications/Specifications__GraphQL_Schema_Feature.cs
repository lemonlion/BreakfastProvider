using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

[FeatureDescription($"/{Endpoints.GraphQLContract.SchemaJson}; /{Endpoints.GraphQLContract.SchemaDefinition} - Serving the GraphQL schema as introspection JSON and as a schema definition")]
public partial class Specifications__GraphQL_Schema_Feature
{
    [HappyPath]
    [Scenario]
    [Trait("Produces", "graphql.json")]
    public async Task The_GraphQL_Schema_Endpoint_Should_Return_A_Valid_Specification()
    {
        await Runner.RunScenarioAsync(
            when => The_graphql_schema_endpoint_is_called(),
            then => The_response_should_be_valid(),
            and => The_schema_should_contain_all_the_reporting_queries(),
            and => The_reporting_queries_should_be_documented(),
            and => The_graphql_schema_is_written_to_disk());
    }

    [HappyPath]
    [Scenario]
    [Trait("Produces", "schema.graphql")]
    public async Task The_GraphQL_Schema_Definition_Endpoint_Should_Return_The_Schema_Definition()
    {
        await Runner.RunScenarioAsync(
            when => The_graphql_schema_definition_endpoint_is_called(),
            then => The_response_should_be_a_graphql_schema_definition(),
            and => The_schema_definition_should_declare_all_the_reporting_queries(),
            and => The_graphql_schema_definition_is_written_to_disk());
    }
}
