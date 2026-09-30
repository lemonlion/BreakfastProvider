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
}
