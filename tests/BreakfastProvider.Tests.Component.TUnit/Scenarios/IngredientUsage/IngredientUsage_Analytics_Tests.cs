using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientUsage;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientUsage;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.IngredientUsage;

public class IngredientUsage_Analytics_Tests : BaseFixture
{
    private readonly PostIngredientUsageSteps _postSteps;
    private readonly GetIngredientUsageSteps _getSteps;

    public IngredientUsage_Analytics_Tests()
    {
        _postSteps = Get<PostIngredientUsageSteps>();
        _getSteps = Get<GetIngredientUsageSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Recording_ingredient_usage_should_return_the_created_record()
    {
        // Given a valid ingredient usage request
        var ingredientName = $"Flour-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = ingredientName,
            QuantityUsed = 2.5m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };

        // When the usage is recorded
        await _postSteps.Send();

        // Then the response should contain the created record
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        await _postSteps.Response!.IngredientName.Should().BeEqualTo(ingredientName);
        await _postSteps.Response!.QuantityUsed.Should().BeEqualTo(2.5m);
        await _postSteps.Response!.UsageId.Should().NotBeNull();
    }

    [Test]
    public async Task Listing_usage_by_ingredient_should_return_matching_records()
    {
        // Given an ingredient usage record exists
        var ingredientName = $"Flour-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = ingredientName,
            QuantityUsed = 1.0m,
            Unit = "kg",
            RecipeName = "Waffles"
        };
        await _postSteps.Send();
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);

        // When the usage is listed by ingredient
        await _getSteps.RetrieveByIngredient(ingredientName);

        // Then the list should contain the record
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        await _getSteps.ListResponse!.Should().Contain(u => u.IngredientName == ingredientName);
    }

    [Test]
    public async Task Getting_usage_summary_should_return_aggregated_data()
    {
        // Given an ingredient usage record exists
        var ingredientName = $"Flour-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = ingredientName,
            QuantityUsed = 3.0m,
            Unit = "kg",
            RecipeName = "Bread"
        };
        await _postSteps.Send();
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);

        // When the summary is requested
        await _getSteps.RetrieveSummary();

        // Then the summary should contain aggregated data
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseSummaryResponse();
        await _getSteps.SummaryResponse!.Should().Contain(s => s.IngredientName == ingredientName);
    }

    [Test]
    public async Task Recording_usage_with_missing_ingredient_name_should_return_bad_request()
    {
        // Given a request with missing ingredient name
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = null,
            QuantityUsed = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };

        // When submitted
        await _postSteps.Send();

        // Then bad request
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Recording_usage_with_zero_quantity_should_return_bad_request()
    {
        // Given a request with zero quantity
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = "Eggs",
            QuantityUsed = 0,
            Unit = "units",
            RecipeName = "Pancakes"
        };

        // When submitted
        await _postSteps.Send();

        // Then bad request
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.BadRequest);
    }
}
