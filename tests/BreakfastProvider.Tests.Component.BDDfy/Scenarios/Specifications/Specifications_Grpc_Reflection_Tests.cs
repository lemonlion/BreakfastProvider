using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;
using Google.Protobuf.Reflection;
using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Specifications;

public class Specifications_Grpc_Reflection_Tests : BaseFixture
{
    private readonly GrpcReflectionSteps _reflectionSteps;

    public Specifications_Grpc_Reflection_Tests()
    {
        _reflectionSteps = Get<GrpcReflectionSteps>();
        if (Settings.RunAgainstExternalServiceUnderTest)
            _reflectionSteps.InitializeExternal(Settings.ExternalGrpcUrl ?? Settings.ExternalServiceUnderTestUrl!);
        else
            _reflectionSteps.Initialize(AppFactory, CurrentTestInfo.Fetcher);
    }

    private FileDescriptorProto DescribedFile => _reflectionSteps.DescribedFiles.Single(f => f.Name == GrpcContractDefaults.ProtoFileName);

    [Fact]
    [HappyPath]
    public void Grpc_server_reflection_should_list_the_breakfast_service()
    {
        this.When(x => x.The_services_are_listed_through_grpc_server_reflection())
            .Then(x => x.The_breakfast_service_should_be_listed())
            .And(x => x.The_reflection_service_should_be_listed())
            .BDDfy();
    }

    [Fact]
    [HappyPath]
    public void Grpc_server_reflection_should_describe_the_breakfast_service()
    {
        this.When(x => x.The_breakfast_service_is_described_through_grpc_server_reflection())
            .Then(x => x.The_description_should_be_the_breakfast_proto_file())
            .And(x => x.The_description_should_contain_every_breakfast_method())
            .BDDfy();
    }

    #region Steps

    private async Task The_services_are_listed_through_grpc_server_reflection()
    {
        await _reflectionSteps.ListServices();
    }

    private void The_breakfast_service_should_be_listed()
    {
        _reflectionSteps.ListedServices.Should().Contain(GrpcContractDefaults.ServiceFullName);
    }

    private void The_reflection_service_should_be_listed()
    {
        _reflectionSteps.ListedServices.Should().Contain(GrpcContractDefaults.ReflectionServiceFullName);
    }

    private async Task The_breakfast_service_is_described_through_grpc_server_reflection()
    {
        await _reflectionSteps.DescribeSymbol(GrpcContractDefaults.ServiceFullName);
    }

    private void The_description_should_be_the_breakfast_proto_file()
    {
        _reflectionSteps.DescribedFiles.Should().ContainSingle(f => f.Name == GrpcContractDefaults.ProtoFileName);
        DescribedFile.Package.Should().Be(GrpcContractDefaults.Package);
    }

    private void The_description_should_contain_every_breakfast_method()
    {
        var service = DescribedFile.Service.Should().ContainSingle(s => s.Name == GrpcContractDefaults.ServiceName).Which;
        service.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetRecipeSummary);
        service.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetOrderStatus);
        service.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.StreamOrderUpdates);
    }

    #endregion
}
