using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.Hooks;

[Binding]
public sealed class ScenarioHooks(AppManager appManager)
{
    [BeforeScenario(Order = 100)]
    public void EnsureDefaultApp()
    {
        // Only create the default app if no Given step will create a custom one.
        // Step definitions that need custom config call appManager.SetDelayedCreation()
        // in a [BeforeScenario] hook with lower order, or create the app in the Given step.
        if (!AppManager.Settings.RunAgainstExternalServiceUnderTest)
            appManager.EnsureDefaultApp();
        else
            appManager.EnsureDefaultApp();
    }

    [AfterScenario]
    public void Cleanup()
    {
        appManager.Dispose();
    }
}
