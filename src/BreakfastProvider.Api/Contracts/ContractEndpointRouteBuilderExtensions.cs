using System.Net.Mime;
using HotChocolate.Execution;

namespace BreakfastProvider.Api.Contracts;

/// <summary>
/// Maps the published contracts beyond OpenAPI and AsyncAPI. They are documentation, not API, so they stay out of the
/// OpenAPI document.
/// </summary>
public static class ContractEndpointRouteBuilderExtensions
{
    /// <summary>
    /// The gRPC contract as a descriptor set in protobuf's JSON mapping, as its proto file, and as the documentation
    /// page at /grpc/.
    /// </summary>
    public static IEndpointRouteBuilder MapGrpcContract(this IEndpointRouteBuilder app)
    {
        app.MapGet(Documentation.Contracts.GrpcJson, () => Results.Text(GrpcContract.Json, MediaTypeNames.Application.Json))
            .ExcludeFromDescription();
        app.MapGet(Documentation.Contracts.GrpcProto, () => Results.Text(GrpcContract.Proto, MediaTypeNames.Text.Plain))
            .ExcludeFromDescription();

        // One route matches /grpc and /grpc/. The page is served on the trailing-slash form, so its relative links
        // (v1.json, protos/breakfast.proto) resolve the same way here and on GitHub Pages; /grpc redirects to it.
        app.MapGet(Documentation.Contracts.GrpcUi, (HttpContext context) =>
                context.Request.Path.Value!.EndsWith('/')
                    ? Results.Text(GrpcContract.Html, MediaTypeNames.Text.Html)
                    : Results.Redirect($"{context.Request.PathBase}{context.Request.Path}/", permanent: true))
            .ExcludeFromDescription();
        return app;
    }

    /// <summary>The GraphQL contract as the standard introspection response, in every environment.</summary>
    public static IEndpointRouteBuilder MapGraphQLSchemaJson(this IEndpointRouteBuilder app)
    {
        app.MapGet(Documentation.Contracts.GraphQLSchemaJson, async (IRequestExecutorResolver executors, CancellationToken ct) =>
                Results.Text(await GraphQLContract.IntrospectAsync(executors, ct), MediaTypeNames.Application.Json))
            .ExcludeFromDescription();
        return app;
    }
}
