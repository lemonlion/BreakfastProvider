using System.Net;

namespace BreakfastProvider.Tests.Component.Shared.Util;

/// <summary>
/// Reads the gRPC documentation page the service renders: each method, field and enum value is a table row whose first
/// cell is its name in <c>&lt;code&gt;</c>.
/// </summary>
public static class GrpcContractPageRows
{
    /// <summary>The table row the name opens, or null when the page has none.</summary>
    public static string? RowOf(string html, string name)
    {
        var start = html.IndexOf($"<tr><td><code>{WebUtility.HtmlEncode(name)}</code></td>", StringComparison.Ordinal);
        if (start < 0)
            return null;
        var end = html.IndexOf("</tr>", start, StringComparison.Ordinal);
        return end < 0 ? null : html[start..(end + "</tr>".Length)];
    }
}
