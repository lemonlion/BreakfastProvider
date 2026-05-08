using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Grpc;

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

    [Test]
    [HappyPath]
    public async Task Pancake_recipe_summary_should_return_correct_data()
    {
        // When a recipe summary is requested for pancakes via gRPC
        await _grpcSteps.GetRecipeSummary("Pancakes");

        // Then the recipe summary should contain pancake data
        await _grpcSteps.RecipeSummaryReply!.RecipeType.Should().BeEqualTo("Pancakes");
        await _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().BeEqualTo(42);
        await _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEquivalentTo(["Milk", "Flour", "Eggs"]);
    }

    [Test]
    [HappyPath]
    public async Task Waffle_recipe_summary_should_return_correct_data()
    {
        // When a recipe summary is requested for waffles via gRPC
        await _grpcSteps.GetRecipeSummary("Waffles");

        // Then the recipe summary should contain waffle data
        await _grpcSteps.RecipeSummaryReply!.RecipeType.Should().BeEqualTo("Waffles");
        await _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().BeEqualTo(28);
        await _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEquivalentTo(["Milk", "Flour", "Eggs", "Butter"]);
    }

    [Test]
    public async Task Unknown_recipe_type_should_return_zero_batches()
    {
        // When a recipe summary is requested for an unknown type via gRPC
        await _grpcSteps.GetRecipeSummary("Unknown");

        // Then the recipe summary should contain zero batches and no ingredients
        await _grpcSteps.RecipeSummaryReply!.RecipeType.Should().BeEqualTo("Unknown");
        await _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().BeEqualTo(0);
        await _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEmpty();
    }
}
