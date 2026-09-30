using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

[FeatureDescription($"/{Endpoints.GraphQLContract.UI} - Serving the Nitro GraphQL IDE")]
public partial class Specifications__GraphQL_UI_Feature
{
    [HappyPath]
    [Scenario]
    public async Task The_GraphQL_UI_Endpoint_Should_Return_A_Valid_Page()
    {
        await Runner.RunScenarioAsync(
            when => The_graphql_ui_endpoint_is_called(),
            then => The_response_should_be_a_valid_nitro_page(),
            and => The_page_should_be_served_by_the_service_itself());
    }
}
