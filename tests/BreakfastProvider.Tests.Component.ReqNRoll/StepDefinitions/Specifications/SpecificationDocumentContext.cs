using BreakfastProvider.Tests.Component.Shared.Common.Specifications;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Specifications;

/// <summary>What a fetched specification document must be for "the response should be valid" to pass.</summary>
public enum SpecificationDocumentFormat
{
    Json,

    /// <summary>A protobuf FileDescriptorSet in protobuf's JSON mapping: the gRPC contract.</summary>
    DescriptorSet
}

/// <summary>
/// The specification document a scenario's When fetched. It is scenario-scoped and every specification binding
/// shares it, so the shared "the response should be valid" checks the document that was actually fetched, in the
/// form the When expected, and fails when no document was fetched at all.
/// </summary>
public sealed class SpecificationDocumentContext(SpecificationDocumentSteps documentSteps)
{
    public SpecificationDocumentSteps Document { get; } = documentSteps;

    /// <summary>Null until a When fetches a document.</summary>
    public SpecificationDocumentFormat? Format { get; private set; }

    public async Task Retrieve(string path, SpecificationDocumentFormat format)
    {
        Format = format;
        await Document.Retrieve(path);
    }
}
