using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

[FeatureDescription($"/{Endpoints.GrpcContract.UI} - Serving the gRPC documentation page")]
public partial class Specifications__Grpc_UI_Feature
{
    [HappyPath]
    [Scenario]
    [Trait("Produces", "grpc.html")]
    public async Task The_Grpc_UI_Endpoint_Should_Return_A_Page_Describing_The_Service()
    {
        await Runner.RunScenarioAsync(
            when => The_grpc_ui_endpoint_is_called(),
            then => The_response_should_be_a_valid_grpc_documentation_page(),
            and => The_page_should_describe_every_breakfast_method(),
            and => The_page_should_link_to_the_grpc_contract(),
            and => The_grpc_ui_page_is_written_to_disk());
    }
}
