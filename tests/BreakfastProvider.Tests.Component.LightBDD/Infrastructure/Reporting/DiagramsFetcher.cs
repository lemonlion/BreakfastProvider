using TestTrackingDiagrams;
using TestTrackingDiagrams.PlantUml;
using TestTrackingDiagrams.Tracking;

namespace BreakfastProvider.Tests.Component.LightBDD.Infrastructure.Reporting;

public static class DiagramsFetcher
{
    private static DefaultDiagramsFetcher.DiagramAsCode[]? _diagrams;

    public static Func<DefaultDiagramsFetcher.DiagramAsCode[]> GetDiagramsFetcher(string plantUmlServerBaseUrl, Func<string, string>? processor = null)
    {
        if (_diagrams is not null)
            return () => _diagrams;

        return () =>
        {
            var perTestId = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
                RequestResponseLogger.RequestAndResponseLogs.Where(x => !(x?.TrackingIgnore ?? true)),
                requestPostFormattingProcessor: processor,
                responsePostFormattingProcessor: processor).ToArray();
            return _diagrams = perTestId
                .SelectMany(test => test.PlantUmls.Select(plantUml =>
                    new DefaultDiagramsFetcher.DiagramAsCode(test.TestId,
                        $"{plantUmlServerBaseUrl}/png/{plantUml.PlantUmlEncoded}",
                        plantUml.PlainText)))
                .ToArray();
        };
    }
}
