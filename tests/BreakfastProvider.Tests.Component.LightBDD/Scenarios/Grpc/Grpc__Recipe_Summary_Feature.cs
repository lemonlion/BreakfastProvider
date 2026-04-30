using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Grpc;

[FeatureDescription("/grpc - Retrieving recipe summaries via gRPC")]
public partial class Grpc__Recipe_Summary_Feature
{
    #region Happy Path

    [HappyPath]
    [Scenario]
    public async Task Pancake_Recipe_Summary_Should_Return_Correct_Data()
    {
        await Runner.RunScenarioAsync(
            when => A_recipe_summary_is_requested_for_pancakes_via_grpc(),
            then => The_recipe_summary_should_contain_pancake_data());
    }

    [HappyPath]
    [Scenario]
    public async Task Waffle_Recipe_Summary_Should_Return_Correct_Data()
    {
        await Runner.RunScenarioAsync(
            when => A_recipe_summary_is_requested_for_waffles_via_grpc(),
            then => The_recipe_summary_should_contain_waffle_data());
    }

    #endregion

    #region Edge Cases

    [Scenario]
    public async Task Unknown_Recipe_Type_Should_Return_Zero_Batches()
    {
        await Runner.RunScenarioAsync(
            when => A_recipe_summary_is_requested_for_an_unknown_type_via_grpc(),
            then => The_recipe_summary_should_contain_zero_batches_and_no_ingredients());
    }

    #endregion
}
