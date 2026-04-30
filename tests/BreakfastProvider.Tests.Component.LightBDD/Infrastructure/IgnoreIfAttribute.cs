using BreakfastProvider.Tests.Component.LightBDD.LightBddCustomisations;

namespace BreakfastProvider.Tests.Component.LightBDD.Infrastructure;

public class IgnoreIfAttribute(string settingName, params string[] reasons) : IgnoreScenarioIfAttribute<ComponentTestSettings>(settingName, reasons)
{ }
