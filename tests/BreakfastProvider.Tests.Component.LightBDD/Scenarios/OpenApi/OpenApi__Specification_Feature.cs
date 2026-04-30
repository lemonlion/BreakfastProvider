using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.OpenApi;

[FeatureDescription($"/{Endpoints.Swagger.SwaggerJson} - Serving the OpenAPI specification describing all REST endpoints")]
public partial class OpenApi__Specification_Feature
{
    [HappyPath]
    [Scenario]
    [Trait("Produces", "openapi.json")]
    public async Task The_OpenApi_Endpoint_Should_Return_A_Valid_Specification()
    {
        await Runner.RunScenarioAsync(
            when => The_open_api_endpoint_is_called(),
            then => The_response_should_be_valid(),
            and => The_response_should_contain_all_the_endpoints(),
            and => The_openapi_spec_is_written_to_disk());
    }
}
