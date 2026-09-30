using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace BreakfastProvider.Api.Contracts;

/// <summary>
/// The gRPC contract this service offers: the descriptor set protoc wrote for breakfast.proto at build time, comments
/// included, and the proto file itself. Both are embedded by the EmbedGrpcContract target in the project file.
/// </summary>
public static class GrpcContract
{
    private static readonly Lazy<FileDescriptorSet> Descriptors = new(LoadDescriptorSet);
    private static readonly Lazy<string> JsonText = new(() =>
        new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation("  ")).Format(Descriptors.Value));
    private static readonly Lazy<string> ProtoText = new(() =>
        ReadResource("Contracts.breakfast.proto").ReplaceLineEndings("\n"));

    public static string Json => JsonText.Value;
    public static string Proto => ProtoText.Value;

    private static FileDescriptorSet LoadDescriptorSet()
    {
        using var stream = OpenResource("Contracts.breakfast.protoset");
        var set = FileDescriptorSet.Parser.ParseFrom(stream);
        foreach (var file in set.File.Where(f => f.SourceCodeInfo is not null))
        {
            // protoc records a location for every token; a reader needs only those that carry a comment.
            var commented = file.SourceCodeInfo.Location
                .Where(l => l.HasLeadingComments || l.HasTrailingComments || l.LeadingDetachedComments.Count > 0)
                .ToList();
            file.SourceCodeInfo.Location.Clear();
            file.SourceCodeInfo.Location.Add(commented);
        }
        return set;
    }

    private static Stream OpenResource(string name) =>
        typeof(GrpcContract).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"{name} is not embedded; see the EmbedGrpcContract target.");

    private static string ReadResource(string name)
    {
        using var reader = new StreamReader(OpenResource(name));
        return reader.ReadToEnd();
    }
}
