using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

[FeatureDescription($"/{Endpoints.Swagger.ScalarUI} - Serving the interactive API documentation UI powered by Scalar")]
public partial class Specifications__Open_Api_Scalar_UI_Feature
{
    [HappyPath]
    [Scenario]
    public async Task The_Scalar_UI_Endpoint_Should_Return_A_Valid_Page()
    {
        await Runner.RunScenarioAsync(
            when => The_scalar_ui_endpoint_is_called(),
            then => The_response_should_be_a_valid_scalar_page());
    }
}
