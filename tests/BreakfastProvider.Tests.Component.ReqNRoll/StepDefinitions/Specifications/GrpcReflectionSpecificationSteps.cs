using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Google.Protobuf.Reflection;
using Reqnroll;
using Kronikol.ReqNRoll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Specifications;

[Binding]
public class GrpcReflectionSpecificationSteps(AppManager appManager)
{
    private readonly GrpcReflectionSteps _reflectionSteps = new();

    private bool _initialized;

    private FileDescriptorProto DescribedFile => _reflectionSteps.DescribedFiles.Single(f => f.Name == GrpcContractDefaults.ProtoFileName);

    private void EnsureGrpcClient()
    {
        if (_initialized) return;
        _initialized = true;
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest)
            _reflectionSteps.InitializeExternal(AppManager.Settings.ExternalGrpcUrl ?? AppManager.Settings.ExternalServiceUnderTestUrl!);
        else
            _reflectionSteps.Initialize(appManager.AppFactory, CurrentTestInfo.Fetcher);
    }

    [When("the services are listed through grpc server reflection")]
    public async Task WhenTheServicesAreListedThroughGrpcServerReflection()
    {
        EnsureGrpcClient();
        await _reflectionSteps.ListServices();
    }

    [Then("the breakfast service should be listed")]
    public void ThenTheBreakfastServiceShouldBeListed()
    {
        _reflectionSteps.ListedServices.Should().Contain(GrpcContractDefaults.ServiceFullName);
    }

    [Then("the reflection service should be listed")]
    public void ThenTheReflectionServiceShouldBeListed()
    {
        _reflectionSteps.ListedServices.Should().Contain(GrpcContractDefaults.ReflectionServiceFullName);
    }

    [When("the breakfast service is described through grpc server reflection")]
    public async Task WhenTheBreakfastServiceIsDescribedThroughGrpcServerReflection()
    {
        EnsureGrpcClient();
        await _reflectionSteps.DescribeSymbol(GrpcContractDefaults.ServiceFullName);
    }

    [Then("the description should be the breakfast proto file")]
    public void ThenTheDescriptionShouldBeTheBreakfastProtoFile()
    {
        _reflectionSteps.DescribedFiles.Should().ContainSingle(f => f.Name == GrpcContractDefaults.ProtoFileName);
        DescribedFile.Package.Should().Be(GrpcContractDefaults.Package);
    }

    [Then("the description should contain every breakfast method")]
    public void ThenTheDescriptionShouldContainEveryBreakfastMethod()
    {
        var service = DescribedFile.Service.Should().ContainSingle(s => s.Name == GrpcContractDefaults.ServiceName).Which;
        service.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetRecipeSummary);
        service.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.GetOrderStatus);
        service.Method.Select(m => m.Name).Should().Contain(GrpcContractDefaults.StreamOrderUpdates);
    }
}
