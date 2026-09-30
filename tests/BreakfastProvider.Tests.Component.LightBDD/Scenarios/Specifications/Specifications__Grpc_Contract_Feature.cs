using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

[FeatureDescription($"/{Endpoints.GrpcContract.ContractJson}; /{Endpoints.GrpcContract.ProtoFile} - Serving the gRPC contract as a descriptor set and as its proto file")]
public partial class Specifications__Grpc_Contract_Feature
{
    [HappyPath]
    [Scenario]
    [Trait("Produces", "grpc.json")]
    public async Task The_Grpc_Contract_Endpoint_Should_Return_A_Valid_Specification()
    {
        await Runner.RunScenarioAsync(
            when => The_grpc_contract_endpoint_is_called(),
            then => The_response_should_be_valid(),
            and => The_grpc_contract_should_describe_the_breakfast_service(),
            and => Every_breakfast_method_should_be_documented(),
            and => The_grpc_contract_is_written_to_disk());
    }

    [HappyPath]
    [Scenario]
    public async Task The_Grpc_Proto_Endpoint_Should_Return_The_Proto_File()
    {
        await Runner.RunScenarioAsync(
            when => The_grpc_proto_endpoint_is_called(),
            then => The_response_should_be_a_plain_text_proto_file(),
            and => The_proto_file_should_declare_the_breakfast_service());
    }
}
