using Google.Protobuf.Reflection;

namespace BreakfastProvider.Tests.Component.Shared.Util;

/// <summary>
/// Reads the comments protoc recorded in a descriptor set. A sourceCodeInfo location addresses the element it
/// documents by field numbers of descriptor.proto: [6, s] is service s of a file, [6, s, 2, m] its method m.
/// </summary>
public static class GrpcDescriptorComments
{
    private const int FileServiceField = 6;      // FileDescriptorProto.service
    private const int ServiceMethodField = 2;    // ServiceDescriptorProto.method

    /// <summary>The methods of the service that carry no leading comment in the proto.</summary>
    public static IReadOnlyList<string> UndocumentedMethods(FileDescriptorProto file, string serviceName)
    {
        var s = file.Service.Select(service => service.Name).ToList().IndexOf(serviceName);
        if (s < 0)
            throw new InvalidOperationException($"{file.Name} declares no service {serviceName}.");
        var service = file.Service[s];
        return service.Method
            .Where((_, m) => string.IsNullOrWhiteSpace(LeadingComment(file, FileServiceField, s, ServiceMethodField, m)))
            .Select(method => method.Name)
            .ToList();
    }

    private static string? LeadingComment(FileDescriptorProto file, params int[] path) =>
        file.SourceCodeInfo?.Location.FirstOrDefault(l => l.Path.SequenceEqual(path))?.LeadingComments;
}
