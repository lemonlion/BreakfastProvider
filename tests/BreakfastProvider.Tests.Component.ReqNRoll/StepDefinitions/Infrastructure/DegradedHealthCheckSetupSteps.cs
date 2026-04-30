using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;
using Reqnroll.Bindings;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Infrastructure;

[Binding]
public class DegradedHealthCheckSetupSteps(AppManager appManager, ScenarioContext scenarioContext)
{
    private readonly List<string> _degradedChecks = [];
    private bool _appCreated;

    private void EnsureAppCreated()
    {
        if (_appCreated) return;

        appManager.CreateAppWithOverrides(additionalServices: services =>
        {
            foreach (var checkName in _degradedChecks)
                services.ReplaceHealthCheckWithDegraded(checkName, $"{checkName} is unreachable (simulated for test).");

            if (!AppManager.Settings.RunWithAnInMemoryDatabase)
                services.ReplaceCosmosDbHealthCheckWithNoOp();

            if (!AppManager.Settings.RunWithAnInMemoryKafkaBroker)
                services.ReplaceKafkaHealthCheckWithNoOp();
        });

        _appCreated = true;
    }

    [Given("the cow service is configured to be unreachable")]
    public void GivenTheCowServiceIsConfiguredToBeUnreachable()
    {
        _degradedChecks.Add(HealthCheckNames.CowService);
    }

    [Given("the supplier service is configured to be unreachable")]
    public void GivenTheSupplierServiceIsConfiguredToBeUnreachable()
    {
        _degradedChecks.Add(HealthCheckNames.SupplierService);
    }

    [Given("the kitchen service health check is configured to use a failing endpoint")]
    public void GivenTheKitchenServiceHealthCheckIsConfiguredToUseAFailingEndpoint()
    {
        appManager.CreateAppWithOverrides(additionalServices: services =>
        {
            services.ReplaceHealthCheckWithFailingEndpoint(HealthCheckNames.KitchenService, "health-degraded");
            if (!AppManager.Settings.RunWithAnInMemoryDatabase)
                services.ReplaceCosmosDbHealthCheckWithNoOp();
        });
        _appCreated = true;
    }

    [BeforeStep]
    public void EnsureAppBeforeStep()
    {
        // Only create the app before When/Then steps, not before Given steps.
        // This ensures all Given steps have queued their degraded checks first.
        if (_degradedChecks.Count > 0 && !_appCreated
            && scenarioContext.StepContext.StepInfo.StepDefinitionType != StepDefinitionType.Given)
            EnsureAppCreated();
    }
}
