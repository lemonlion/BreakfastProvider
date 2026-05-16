namespace BreakfastProvider.Tests.Component.Shared.Infrastructure;

public static class SourceControlledDocsHelper
{
    private const string DocsFolder = "../../../../../docs/";

    public static async Task CopySpecificationsFileToDocsFolder(string specificationsFileName = "Specifications")
    {
        var specsPath = $"Reports/{specificationsFileName}.yml";
        if (!File.Exists(specsPath)) return;

        var specs = await File.ReadAllTextAsync(specsPath);
        if (specs.Length is not 0)
        {
            specs = specs.Replace("\r\n", "\n");
            await File.WriteAllTextAsync($"{DocsFolder}{specificationsFileName}.yml", specs);
        }
    }

    public static async Task CopyApiSpecificationFilesToDocsFolder()
    {
        await CopyReportAttachmentToDocs("openapi.json");
        await CopyReportAttachmentToDocs("asyncapi.json");
    }

    private static async Task CopyReportAttachmentToDocs(string fileName)
    {
        var sourcePath = $"Reports/attachments/{fileName}";
        if (!File.Exists(sourcePath)) return;

        var content = await File.ReadAllTextAsync(sourcePath);
        if (content.Length is not 0)
        {
            content = content.Replace("\r\n", "\n");
            await File.WriteAllTextAsync($"{DocsFolder}{fileName}", content);
        }
    }
}
