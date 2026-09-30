namespace BreakfastProvider.Tests.Component.Shared.Constants;

/// <summary>What the published gRPC contract (breakfast.proto) declares.</summary>
public static class GrpcContractDefaults
{
    public const string ProtoFileName = "breakfast.proto";
    public const string Package = "breakfast";
    public const string ServiceName = "BreakfastGrpc";
    public const string ServiceFullName = $"{Package}.{ServiceName}";
    public const string ReflectionServiceFullName = "grpc.reflection.v1.ServerReflection";

    public const string GetRecipeSummary = "GetRecipeSummary";
    public const string GetOrderStatus = "GetOrderStatus";
    public const string StreamOrderUpdates = "StreamOrderUpdates";

    /// <summary>Part of the service's comment in the proto, as the documentation page shows it.</summary>
    public const string ServiceDescription = "recipe summaries and order status for kitchen systems";
    public const string ServerStreamingKind = "server streaming";

    /// <summary>The documents the documentation page links to, relative to the page.</summary>
    public const string ContractJsonLink = "v1.json";
    public const string ProtoFileLink = "protos/breakfast.proto";
}
