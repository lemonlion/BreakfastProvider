using LightBDD.Core.Execution;
using LightBDD.Core.Extensibility.Execution;
using LightBDD.Framework;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.LightBddCustomisations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class IgnoreScenarioIfAttribute<T>(string settingName, params string[] reasons) : Attribute, IScenarioDecoratorAttribute where T : class
{
    public Task ExecuteAsync(IScenario scenario, Func<Task> scenarioInvocation)
    {
        var settingField = typeof(T).GetProperty(settingName);
        var settingObjectValue = settingField?.GetValue((scenario.Fixture as IIgnorable<T>)?.IgnoreSettings);

        var shouldIgnore = settingObjectValue is true;

        if (!shouldIgnore)
            return scenarioInvocation.Invoke();

        StepExecution.Current.IgnoreScenario(string.Join("; ", reasons));
        return Task.CompletedTask;
    }

    public int Order => 1;
}
