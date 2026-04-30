using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using Reqnroll;
using Xunit;

namespace BreakfastProvider.Tests.Component.ReqNRoll.Hooks;

[Binding]
public sealed class ConditionalIgnoreHooks
{
    private static ComponentTestSettings Settings => AppManager.Settings;

    [BeforeScenario("IgnoreIfExternalSut")]
    public void IgnoreIfExternalSut()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            Assert.Skip("Skipped: " + NeedsNonDefaultConfiguration);
    }

    [BeforeScenario("IgnoreIfNeedsEventInfrastructure")]
    public void IgnoreIfNeedsEventInfrastructure()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            Assert.Skip("Skipped: " + NeedsEventAndKafkaInfrastructure);
    }

    [BeforeScenario("SkipUnlessFakesControllable")]
    public void SkipUnlessFakesControllable()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            Assert.Skip("Skipped: " + NeedsToControlFakeResponses);
    }

    [BeforeScenario("IgnoreUnlessInMemoryDb")]
    public void IgnoreUnlessInMemoryDb()
    {
        if (!Settings.RunWithAnInMemoryDatabase)
            Assert.Skip("Skipped: " + NeedsIsolatedDatabase);
    }

    [BeforeScenario("IgnoreIfNeedsDirectDbAccess")]
    public void IgnoreIfNeedsDirectDbAccess()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            Assert.Skip("Skipped: " + NeedsDirectDatabaseAccess);
    }
}
