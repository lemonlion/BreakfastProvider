using BreakfastProvider.Tests.Component.LightBDD.LightBddCustomisations;

namespace BreakfastProvider.Tests.Component.LightBDD.Infrastructure;

public class SkipStepIfAttribute(string settingName, params string[] reasons) : SkipStepIfAttribute<ComponentTestSettings>(settingName, reasons)
{ }
