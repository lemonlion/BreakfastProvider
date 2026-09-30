using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using Kronikol.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Specifications;

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

    [Fact]
    [HappyPath]
    public async Task Grpc_server_reflection_should_list_the_breakfast_service()
    {
        // When the services are listed through grpc server reflection
        await _reflectionSteps.ListServices();

        // Then the breakfast service should be listed
        _reflectionSteps.ListedServices.Should().Contain(GrpcContractDefaults.ServiceFullName);

        // And the reflection service should be listed
        _reflectionSteps.ListedServices.Should().Contain(GrpcContractDefaults.ReflectionServiceFullName);
    }

    [Fact]
    [HappyPath]
    public async Task Grpc_server_reflection_should_describe_the_breakfast_service()
    {
        // When the breakfast service is described through grpc server reflection
        await _reflectionSteps.DescribeSymbol(GrpcContractDefaults.ServiceFullName);

        // Then the description should be the breakfast proto file
        var file = _reflectionSteps.DescribedFiles.Should().ContainSingle(f => f.Name == GrpcContractDefaults.ProtoFileName).Which;
        file.Package.Should().Be(GrpcContractDefaults.Package);

        // And the description should contain every breakfast method
        var service = file.Service.Should().ContainSingle(s => s.Name == GrpcContractDefaults.ServiceName).Which;
        service.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetRecipeSummary);
        service.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetOrderStatus);
        service.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.StreamOrderUpdates);
    }
}
