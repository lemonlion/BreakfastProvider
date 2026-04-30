using BreakfastProvider.Tests.Component.LightBDD.LightBddCustomisations;

namespace BreakfastProvider.Tests.Component.LightBDD.Infrastructure;

public class IgnoreUnlessAttribute(string settingName, params string[] reasons) : IgnoreScenarioUnlessAttribute<ComponentTestSettings>(settingName, reasons)
{ }
