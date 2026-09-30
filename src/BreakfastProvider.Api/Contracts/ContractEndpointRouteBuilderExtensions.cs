using System.Net.Mime;

namespace BreakfastProvider.Api.Contracts;

/// <summary>
/// Maps the published contracts beyond OpenAPI and AsyncAPI. They are documentation, not API, so they stay out of the
/// OpenAPI document.
/// </summary>
public static class ContractEndpointRouteBuilderExtensions
{
    /// <summary>The gRPC contract as a descriptor set in protobuf's JSON mapping, and as its proto file.</summary>
    public static IEndpointRouteBuilder MapGrpcContract(this IEndpointRouteBuilder app)
    {
        app.MapGet(Documentation.Contracts.GrpcJson, () => Results.Text(GrpcContract.Json, MediaTypeNames.Application.Json))
            .ExcludeFromDescription();
        app.MapGet(Documentation.Contracts.GrpcProto, () => Results.Text(GrpcContract.Proto, MediaTypeNames.Text.Plain))
            .ExcludeFromDescription();
        return app;
    }
}
