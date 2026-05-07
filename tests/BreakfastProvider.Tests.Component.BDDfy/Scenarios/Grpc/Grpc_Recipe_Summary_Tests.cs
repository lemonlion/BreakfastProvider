using BreakfastProvider.Tests.Component.Shared.Common.Grpc;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Grpc;

public class Grpc_Recipe_Summary_Tests : BaseFixture
{
    private readonly GrpcBreakfastSteps _grpcSteps;

    public Grpc_Recipe_Summary_Tests()
    {
        _grpcSteps = Get<GrpcBreakfastSteps>();
        if (Settings.RunAgainstExternalServiceUnderTest)
            _grpcSteps.InitializeExternal(Settings.ExternalGrpcUrl ?? Settings.ExternalServiceUnderTestUrl!);
        else
            _grpcSteps.Initialize(AppFactory, CurrentTestInfo.Fetcher);
    }

    [Fact]
    [HappyPath]
    public void Pancake_recipe_summary_should_return_correct_data()
    {
        this.When(x => x.A_recipe_summary_is_requested_for_pancakes())
            .Then(x => x.The_recipe_summary_should_contain_pancake_data())
            .BDDfy();
    }

    [Fact]
    [HappyPath]
    public void Waffle_recipe_summary_should_return_correct_data()
    {
        this.When(x => x.A_recipe_summary_is_requested_for_waffles())
            .Then(x => x.The_recipe_summary_should_contain_waffle_data())
            .BDDfy();
    }

    [Fact]
    public void Unknown_recipe_type_should_return_zero_batches()
    {
        this.When(x => x.A_recipe_summary_is_requested_for_an_unknown_type())
            .Then(x => x.The_recipe_summary_should_contain_zero_batches_and_no_ingredients())
            .BDDfy();
    }

    #region Steps

    private async Task A_recipe_summary_is_requested_for_pancakes()
    {
        await _grpcSteps.GetRecipeSummary("Pancakes");
    }

    private void The_recipe_summary_should_contain_pancake_data()
    {
        _grpcSteps.RecipeSummaryReply!.RecipeType.Should().Be("Pancakes");
        _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().Be(42);
        _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEquivalentTo(["Milk", "Flour", "Eggs"]);
    }

    private async Task A_recipe_summary_is_requested_for_waffles()
    {
        await _grpcSteps.GetRecipeSummary("Waffles");
    }

    private void The_recipe_summary_should_contain_waffle_data()
    {
        _grpcSteps.RecipeSummaryReply!.RecipeType.Should().Be("Waffles");
        _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().Be(28);
        _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEquivalentTo(["Milk", "Flour", "Eggs", "Butter"]);
    }

    private async Task A_recipe_summary_is_requested_for_an_unknown_type()
    {
        await _grpcSteps.GetRecipeSummary("Unknown");
    }

    private void The_recipe_summary_should_contain_zero_batches_and_no_ingredients()
    {
        _grpcSteps.RecipeSummaryReply!.RecipeType.Should().Be("Unknown");
        _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().Be(0);
        _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEmpty();
    }

    #endregion
}
