using System.Text.RegularExpressions;
using MoreEnumerable = MoreLinq.MoreEnumerable;

namespace BreakfastProvider.Tests.Component.LightBDD.LightBddCustomisations;

public static class HtmlDocumentHelpers
{
    public const int PlantUmlRequestOrResponseApproximateMaxCharLength = 80_000;

    public static Func<string, string> AccessTokenShortenerProcessor => content =>
    {
        var tokenRegex = new Regex("(?<=\"milk\": \")[^\"]+(?=\")");

        content = content.RedactEnding(tokenRegex)
                         .SplitWordsOverMaxLength();

        if (content.Length > PlantUmlRequestOrResponseApproximateMaxCharLength)
            content = content[..PlantUmlRequestOrResponseApproximateMaxCharLength] + "_RedactedEnding";

        return content;
    };

    private static string RedactEnding(this string value, Regex regex) => regex.Replace(value,
        m => m.Value.Length > 200 ? m.Value[..30] + "_RedactedEnding" : m.Value);

    private static string SplitWordsOverMaxLength(this string value, int maxLength = 200)
    {
        var words = value.Split("\n").Select(x => x.Trim().Split(' ')).SelectMany(x => x).Where(x => !string.IsNullOrWhiteSpace(x));
        var wordsTooLong = words.Where(x => x.Length > maxLength);
        wordsTooLong.ToList().ForEach(word =>
        {
            var splitWords = MoreEnumerable.Batch(word, maxLength).Select(x => new string(x)).ToList();
            value = value.Replace(word, string.Join("\n", splitWords));
        });
        return value;
    }
}
