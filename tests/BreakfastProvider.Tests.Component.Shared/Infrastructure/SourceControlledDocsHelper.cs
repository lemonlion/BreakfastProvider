using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure;

public static class SourceControlledDocsHelper
{
    public static async Task CopySpecificationsFileToDocsFolder(string specificationsFileName = "Specifications")
    {
        var specsPath = $"Reports/{specificationsFileName}.yml";
        if (!File.Exists(specsPath)) return;

        var specs = await File.ReadAllTextAsync(specsPath);
        if (specs.Length is not 0)
        {
            specs = specs.Replace("\r\n", "\n");
            await File.WriteAllTextAsync($"{ContractDocs.FolderPath}{specificationsFileName}.yml", specs);
        }
    }

    /// <summary>
    /// Copies each contract the run attached to its report over docs/, with the same normalisation as the scenario
    /// that wrote it. A contract the run did not produce is left alone.
    /// </summary>
    public static async Task CopyApiSpecificationFilesToDocsFolder()
    {
        foreach (var fileName in ContractDocs.FileNames)
            await CopyReportAttachmentToDocs(fileName);
    }

    private static async Task CopyReportAttachmentToDocs(string fileName)
    {
        var sourcePath = $"Reports/attachments/{fileName}";
        if (!File.Exists(sourcePath)) return;

        var content = await File.ReadAllTextAsync(sourcePath);
        if (content.Length is not 0)
            await ContractDocs.WriteAsync(fileName, content);
    }
}
