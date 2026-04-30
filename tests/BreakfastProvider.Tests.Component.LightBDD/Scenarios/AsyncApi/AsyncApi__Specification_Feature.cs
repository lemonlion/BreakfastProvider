using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.AsyncApi;

[FeatureDescription($"{Endpoints.AsyncApi.AsyncApiSpec} - Serving the AsyncAPI specification describing event-driven messaging")]
public partial class AsyncApi__Specification_Feature
{
    [HappyPath]
    [Scenario]
    [Trait("Produces", "asyncapi.json")]
    public async Task The_AsyncApi_Endpoint_Should_Return_A_Valid_Specification()
    {
        await Runner.RunScenarioAsync(
            when => The_asyncapi_endpoint_is_called(),
            then => The_response_should_be_valid(),
            and => The_asyncapi_spec_is_written_to_disk());
    }
}