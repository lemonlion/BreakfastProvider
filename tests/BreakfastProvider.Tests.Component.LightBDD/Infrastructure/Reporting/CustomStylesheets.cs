namespace BreakfastProvider.Tests.Component.LightBDD.Infrastructure.Reporting;

public class CustomStylesheets
{
    private static string? _specificationsDevPortalStyleSheet;

    public static string GetSpecificationsDevPortalStyleSheet() =>
        _specificationsDevPortalStyleSheet ??= File.ReadAllText("Stylesheets/specifications-dev-portal.css");
}
