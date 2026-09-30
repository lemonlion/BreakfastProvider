using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

[FeatureDescription("gRPC server reflection - Letting gRPC tools discover the breakfast service")]
public partial class Specifications__Grpc_Reflection_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Grpc_Server_Reflection_Should_List_The_Breakfast_Service()
    {
        await Runner.RunScenarioAsync(
            when => The_services_are_listed_through_grpc_server_reflection(),
            then => The_breakfast_service_should_be_listed(),
            and => The_reflection_service_should_be_listed());
    }

    [HappyPath]
    [Scenario]
    public async Task Grpc_Server_Reflection_Should_Describe_The_Breakfast_Service()
    {
        await Runner.RunScenarioAsync(
            when => The_breakfast_service_is_described_through_grpc_server_reflection(),
            then => The_description_should_be_the_breakfast_proto_file(),
            and => The_description_should_contain_every_breakfast_method());
    }
}
