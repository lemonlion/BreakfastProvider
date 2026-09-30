using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Google.Protobuf.Reflection;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;
using Kronikol.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Specifications__Grpc_Reflection_Feature : BaseFixture
{
    private readonly GrpcReflectionSteps _reflectionSteps;

    public Specifications__Grpc_Reflection_Feature()
    {
        _reflectionSteps = Get<GrpcReflectionSteps>();
        if (Settings.RunAgainstExternalServiceUnderTest)
            _reflectionSteps.InitializeExternal(Settings.ExternalGrpcUrl ?? Settings.ExternalServiceUnderTestUrl!);
        else
            _reflectionSteps.Initialize(AppFactory, CurrentTestInfo.Fetcher);
    }

    private FileDescriptorProto DescribedFile => _reflectionSteps.DescribedFiles.Single(f => f.Name == GrpcContractDefaults.ProtoFileName);
    private ServiceDescriptorProto DescribedService => DescribedFile.Service.Single(s => s.Name == GrpcContractDefaults.ServiceName);

    #region Given
    #endregion

    #region When

    private async Task The_services_are_listed_through_grpc_server_reflection()
        => await _reflectionSteps.ListServices();

    private async Task The_breakfast_service_is_described_through_grpc_server_reflection()
        => await _reflectionSteps.DescribeSymbol(GrpcContractDefaults.ServiceFullName);

    #endregion

    #region Then

    private async Task The_breakfast_service_should_be_listed()
        => _reflectionSteps.ListedServices.Should().Contain(GrpcContractDefaults.ServiceFullName);

    private async Task The_reflection_service_should_be_listed()
        => _reflectionSteps.ListedServices.Should().Contain(GrpcContractDefaults.ReflectionServiceFullName);

    private async Task<CompositeStep> The_description_should_be_the_breakfast_proto_file()
    {
        return Sub.Steps(
            _ => The_description_should_hold_the_FILE_file(GrpcContractDefaults.ProtoFileName),
            _ => The_described_file_should_be_in_the_PACKAGE_package(GrpcContractDefaults.Package));
    }

    private async Task The_description_should_hold_the_FILE_file(string file)
        => _reflectionSteps.DescribedFiles.Should().ContainSingle(f => f.Name == file);

    private async Task The_described_file_should_be_in_the_PACKAGE_package(string package)
        => DescribedFile.Package.Should().Be(package);

    private async Task<CompositeStep> The_description_should_contain_every_breakfast_method()
    {
        return Sub.Steps(
            _ => The_described_file_should_declare_the_SERVICE_service(GrpcContractDefaults.ServiceName),
            _ => The_described_service_should_offer_the_METHOD_method(GrpcContractDefaults.GetRecipeSummary),
            _ => The_described_service_should_offer_the_METHOD_method(GrpcContractDefaults.GetOrderStatus),
            _ => The_described_service_should_offer_the_METHOD_method(GrpcContractDefaults.StreamOrderUpdates));
    }

    private async Task The_described_file_should_declare_the_SERVICE_service(string service)
        => DescribedFile.Service.Should().ContainSingle(s => s.Name == service);

    private async Task The_described_service_should_offer_the_METHOD_method(string method)
        => DescribedService.Method.Select(m => m.Name).Should().Contain(method);

    #endregion
}
