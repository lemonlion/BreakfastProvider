using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientUsage;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientUsage;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using Kronikol.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.IngredientUsage;

public class IngredientUsage_Analytics_Tests : BaseFixture
{
    private readonly PostIngredientUsageSteps _postSteps;
    private readonly GetIngredientUsageSteps _getSteps;

    public IngredientUsage_Analytics_Tests()
    {
        _postSteps = Get<PostIngredientUsageSteps>();
        _getSteps = Get<GetIngredientUsageSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Recording_ingredient_usage_should_return_the_created_record()
    {
        // Given a valid ingredient usage request
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = $"Flour-{Guid.NewGuid():N}",
            QuantityUsed = 2.5m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };

        // When the usage is recorded
        await _postSteps.Send();

        // Then the response should contain the created record
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.IngredientName.Should().Be(_postSteps.Request.IngredientName);
        _postSteps.Response!.QuantityUsed.Should().Be(2.5m);
        _postSteps.Response!.UsageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Listing_usage_by_ingredient_should_return_matching_records()
    {
        // Given an ingredient usage record exists
        var ingredientName = $"Sugar-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = ingredientName,
            QuantityUsed = 1.0m,
            Unit = "kg",
            RecipeName = "Waffles"
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdUsageId = _postSteps.Response!.UsageId;

        // When usage is listed by ingredient
        await _getSteps.RetrieveByIngredient(ingredientName);

        // Then the list should contain the created record
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(u => u.UsageId == createdUsageId);
    }

    [Fact]
    public async Task Getting_usage_summary_should_return_aggregated_data()
    {
        // Given ingredient usage records exist
        var ingredientName = $"Butter-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = ingredientName,
            QuantityUsed = 0.5m,
            Unit = "kg",
            RecipeName = "Muffins"
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

        // When the summary is requested
        await _getSteps.RetrieveSummary();

        // Then the summary should contain aggregated data
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseSummaryResponse();
        _getSteps.SummaryResponse!.Should().Contain(s => s.IngredientName == ingredientName);
    }

    [Fact]
    public async Task Recording_usage_with_missing_ingredient_name_should_return_bad_request()
    {
        // Given an invalid request with missing ingredient name
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = null,
            QuantityUsed = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };

        // When the usage is recorded
        await _postSteps.Send();

        // Then the response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Recording_usage_with_zero_quantity_should_return_bad_request()
    {
        // Given an invalid request with zero quantity
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = "Eggs",
            QuantityUsed = 0,
            Unit = "units",
            RecipeName = "Pancakes"
        };

        // When the usage is recorded
        await _postSteps.Send();

        // Then the response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
