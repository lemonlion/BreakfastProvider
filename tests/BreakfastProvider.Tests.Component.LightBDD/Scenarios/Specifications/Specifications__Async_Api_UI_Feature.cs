using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

[FeatureDescription($"{Endpoints.AsyncApi.AsyncApiUI} - Serving the AsyncAPI documentation UI")]
public partial class Specifications__Async_Api_UI_Feature
{
    [HappyPath]
    [Scenario]
    public async Task The_AsyncApi_UI_Endpoint_Should_Return_A_Valid_Page()
    {
        await Runner.RunScenarioAsync(
            when => The_asyncapi_ui_endpoint_is_called(),
            then => The_response_should_be_a_valid_asyncapi_page());
    }
}
