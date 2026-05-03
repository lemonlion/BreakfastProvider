using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Grpc;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Grpc__Recipe_Summary_Feature : BaseFixture
{
    private readonly GrpcBreakfastSteps _grpcSteps;

    public Grpc__Recipe_Summary_Feature()
    {
        _grpcSteps = Get<GrpcBreakfastSteps>();
        if (Settings.RunAgainstExternalServiceUnderTest)
            _grpcSteps.InitializeExternal(Settings.ExternalServiceUnderTestUrl!);
        else
            _grpcSteps.Initialize(AppFactory, CurrentTestInfo.Fetcher);
    }

    #region When

    private async Task A_recipe_summary_is_requested_for_pancakes_via_grpc()
        => await _grpcSteps.GetRecipeSummary("Pancakes");

    private async Task A_recipe_summary_is_requested_for_waffles_via_grpc()
        => await _grpcSteps.GetRecipeSummary("Waffles");

    private async Task A_recipe_summary_is_requested_for_an_unknown_type_via_grpc()
        => await _grpcSteps.GetRecipeSummary("Unknown");

    #endregion

    #region Then

    private async Task<CompositeStep> The_recipe_summary_should_contain_pancake_data()
    {
        return Sub.Steps(
            _ => The_recipe_type_should_be("Pancakes"),
            _ => The_total_batches_should_be(42),
            _ => The_common_ingredients_should_contain("Milk", "Flour", "Eggs"));
    }

    private async Task<CompositeStep> The_recipe_summary_should_contain_waffle_data()
    {
        return Sub.Steps(
            _ => The_recipe_type_should_be("Waffles"),
            _ => The_total_batches_should_be(28),
            _ => The_common_ingredients_should_contain("Milk", "Flour", "Eggs", "Butter"));
    }

    private async Task<CompositeStep> The_recipe_summary_should_contain_zero_batches_and_no_ingredients()
    {
        return Sub.Steps(
            _ => The_recipe_type_should_be("Unknown"),
            _ => The_total_batches_should_be(0),
            _ => The_common_ingredients_should_be_empty());
    }

    private async Task The_recipe_type_should_be(string expected)
        => Track.That(() => _grpcSteps.RecipeSummaryReply!.RecipeType.Should().Be(expected));

    private async Task The_total_batches_should_be(int expected)
        => Track.That(() => _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().Be(expected));

    private async Task The_common_ingredients_should_contain(params string[] expected)
        => Track.That(() => _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEquivalentTo(expected));

    private async Task The_common_ingredients_should_be_empty()
        => Track.That(() => _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEmpty());

    #endregion
}
