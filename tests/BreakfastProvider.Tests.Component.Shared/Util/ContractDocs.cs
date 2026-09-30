using System.Text;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.Shared.Util;

/// <summary>
/// The contract documents the tests write into docs/ and the Pages site publishes. Every suite writes the same
/// bytes on every OS: UTF-8 without a BOM, LF line ends, and a CRLF that an XML doc comment carried into a JSON
/// string from a Windows checkout folded to LF.
/// </summary>
public static class ContractDocs
{
    public const string FolderPath = "../../../../../docs/";

    public static readonly IReadOnlyList<string> FileNames =
        [OpenApiSpecs.JsonFileName, AsyncApiSpecs.JsonFileName,
         GrpcSpecs.JsonFileName, GrpcSpecs.HtmlFileName,
         GraphQLSpecs.JsonFileName, GraphQLSpecs.SdlFileName];

    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public static string Normalise(string content) =>
        content.Replace("\r\n", "\n").Replace(@"\r\n", @"\n");

    /// <summary>Writes the document into docs/ and returns the full path, for the suite to attach.</summary>
    public static async Task<string> WriteAsync(string fileName, string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        var path = Path.GetFullPath($"{FolderPath}{fileName}");
        const int maxRetries = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, Normalise(content), Utf8WithoutBom);
                return path;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }
}
