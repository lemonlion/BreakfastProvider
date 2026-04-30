using LightBDD.Core.Execution;
using LightBDD.Core.ExecutionContext;
using LightBDD.Core.Extensibility.Execution;
using LightBDD.Framework;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.LightBddCustomisations;

public class SkipStepIfAttribute<T>(string settingName, params string[] reasons) : Attribute, IStepDecoratorAttribute where T : class
{
    public Task ExecuteAsync(IStep step, Func<Task> stepInvocation)
    {
        var settingField = typeof(T).GetProperty(settingName);
        var currentScenarioFixture = ScenarioExecutionContext.CurrentScenario.Fixture;
        var settingObjectValue = settingField?.GetValue((currentScenarioFixture as IIgnorable<T>)?.IgnoreSettings);

        var shouldIgnore = settingObjectValue is true;

        if (!shouldIgnore)
            return stepInvocation.Invoke();

        StepExecution.Current.Bypass(string.Join("; ", reasons));
        return Task.CompletedTask;
    }

    public int Order => 1;
}
