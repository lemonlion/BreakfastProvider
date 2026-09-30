using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Specifications;

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

    [Test]
    [HappyPath]
    public async Task Grpc_server_reflection_should_list_the_breakfast_service()
    {
        // When the services are listed through grpc server reflection
        await _reflectionSteps.ListServices();

        // Then the breakfast service should be listed
        await _reflectionSteps.ListedServices.Should().Contain(s => s == GrpcContractDefaults.ServiceFullName);

        // And the reflection service should be listed
        await _reflectionSteps.ListedServices.Should().Contain(s => s == GrpcContractDefaults.ReflectionServiceFullName);
    }

    [Test]
    [HappyPath]
    public async Task Grpc_server_reflection_should_describe_the_breakfast_service()
    {
        // When the breakfast service is described through grpc server reflection
        await _reflectionSteps.DescribeSymbol(GrpcContractDefaults.ServiceFullName);

        // Then the description should be the breakfast proto file
        await _reflectionSteps.DescribedFiles.Should().Contain(f => f.Name == GrpcContractDefaults.ProtoFileName);
        var file = _reflectionSteps.DescribedFiles.Single(f => f.Name == GrpcContractDefaults.ProtoFileName);
        await file.Package.Should().BeEqualTo(GrpcContractDefaults.Package);

        // And the description should contain every breakfast method
        await file.Service.Should().Contain(s => s.Name == GrpcContractDefaults.ServiceName);
        var service = file.Service.Single(s => s.Name == GrpcContractDefaults.ServiceName);
        await service.Method.Should().Contain(m => m.Name == GrpcContractDefaults.GetRecipeSummary);
        await service.Method.Should().Contain(m => m.Name == GrpcContractDefaults.GetOrderStatus);
        await service.Method.Should().Contain(m => m.Name == GrpcContractDefaults.StreamOrderUpdates);
    }
}
