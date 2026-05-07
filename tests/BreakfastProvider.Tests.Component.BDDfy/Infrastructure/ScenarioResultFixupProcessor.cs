using TestStack.BDDfy;
using TestStack.BDDfy.Configuration;

namespace BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

/// <summary>
/// Fixes BDDfy scenario results for tests that use inline code instead of Given/When/Then step methods.
/// When BDDfy finds no scannable steps, Scenario.Result returns NotExecuted (mapped to Skipped in reports).
/// This processor adds a non-reportable passed marker step so BDDfy correctly reports Passed.
/// </summary>
internal class ScenarioResultFixupProcessor : IProcessor
{
    public ProcessType ProcessType => ProcessType.BeforeReport;

    public void Process(Story story)
    {
        foreach (var scenario in story.Scenarios)
        {
            if (scenario.Steps.Count == 0)
            {
                scenario.Steps.Add(new Step(
                    _ => null!,
                    new StepTitle("Scenario executed inline"),
                    asserts: false,
                    ExecutionOrder.Assertion,
                    shouldReport: false,
                    [])
                { Result = Result.Passed });
            }
        }
    }
}
