using BreakfastProvider.Tests.Component.Shared.Common.Grpc;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Grpc;

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
    public async Task Pancake_recipe_summary_should_return_correct_data()
    {
        // When a recipe summary is requested for pancakes via gRPC
        await _grpcSteps.GetRecipeSummary("Pancakes");

        // Then the recipe summary should contain pancake data
        Track.That(() => _grpcSteps.RecipeSummaryReply!.RecipeType.Should().Be("Pancakes"));
        Track.That(() => _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().Be(42));
        Track.That(() => _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEquivalentTo(["Milk", "Flour", "Eggs"]));
    }

    [Fact]
    [HappyPath]
    public async Task Waffle_recipe_summary_should_return_correct_data()
    {
        // When a recipe summary is requested for waffles via gRPC
        await _grpcSteps.GetRecipeSummary("Waffles");

        // Then the recipe summary should contain waffle data
        Track.That(() => _grpcSteps.RecipeSummaryReply!.RecipeType.Should().Be("Waffles"));
        Track.That(() => _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().Be(28));
        Track.That(() => _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEquivalentTo(["Milk", "Flour", "Eggs", "Butter"]));
    }

    [Fact]
    public async Task Unknown_recipe_type_should_return_zero_batches()
    {
        // When a recipe summary is requested for an unknown type via gRPC
        await _grpcSteps.GetRecipeSummary("Unknown");

        // Then the recipe summary should contain zero batches and no ingredients
        Track.That(() => _grpcSteps.RecipeSummaryReply!.RecipeType.Should().Be("Unknown"));
        Track.That(() => _grpcSteps.RecipeSummaryReply!.TotalBatches.Should().Be(0));
        Track.That(() => _grpcSteps.RecipeSummaryReply!.CommonIngredients.Should().BeEmpty());
    }
}
