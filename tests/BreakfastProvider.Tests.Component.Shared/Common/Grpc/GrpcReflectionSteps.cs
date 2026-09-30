using BreakfastProvider.Api;
using Google.Protobuf.Reflection;
using Grpc.Net.Client;
using Grpc.Reflection.V1;
using Kronikol.Extensions.Grpc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BreakfastProvider.Tests.Component.Shared.Common.Grpc;

/// <summary>
/// Asks the service's gRPC server reflection (grpc.reflection.v1) what it offers, the way grpcurl, Postman and Kreya do.
/// </summary>
public class GrpcReflectionSteps
{
    private ServerReflection.ServerReflectionClient? _client;

    public IReadOnlyList<string> ListedServices { get; private set; } = [];
    public IReadOnlyList<FileDescriptorProto> DescribedFiles { get; private set; } = [];

    public void Initialize<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory,
        Func<(string Name, string Id)> currentTestInfoFetcher) where TEntryPoint : class
    {
        _client = factory.CreateTestTrackingGrpcClient<TEntryPoint, ServerReflection.ServerReflectionClient>(
            new GrpcTrackingOptions
            {
                ServiceName = Documentation.ServiceNames.BreakfastProvider,
                Verbosity = GrpcTrackingVerbosity.Detailed,
                CurrentTestInfoFetcher = currentTestInfoFetcher
            });
    }

    public void InitializeExternal(string baseUrl) =>
        _client = new ServerReflection.ServerReflectionClient(GrpcChannel.ForAddress(baseUrl));

    public async Task ListServices()
    {
        var response = await Ask(new ServerReflectionRequest { ListServices = string.Empty });
        ListedServices = response.ListServicesResponse.Service.Select(s => s.Name).ToList();
    }

    public async Task DescribeSymbol(string fullyQualifiedName)
    {
        var response = await Ask(new ServerReflectionRequest { FileContainingSymbol = fullyQualifiedName });
        DescribedFiles = response.FileDescriptorResponse.FileDescriptorProto
            .Select(FileDescriptorProto.Parser.ParseFrom).ToList();
    }

    private async Task<ServerReflectionResponse> Ask(ServerReflectionRequest request)
    {
        using var call = _client!.ServerReflectionInfo();
        await call.RequestStream.WriteAsync(request);
        await call.RequestStream.CompleteAsync();
        await call.ResponseStream.MoveNext(CancellationToken.None);
        return call.ResponseStream.Current;
    }
}
