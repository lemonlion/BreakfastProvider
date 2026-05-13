using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientUsage;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientUsage;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.IngredientUsage;

public class IngredientUsage_Analytics_Tests : BaseFixture
{
    private readonly PostIngredientUsageSteps _postSteps;
    private readonly GetIngredientUsageSteps _getSteps;

    private string _ingredientName = null!;

    public IngredientUsage_Analytics_Tests()
    {
        _postSteps = Get<PostIngredientUsageSteps>();
        _getSteps = Get<GetIngredientUsageSteps>();
    }

    [Fact]
    [HappyPath]
    public void Recording_ingredient_usage_should_return_the_created_record()
    {
        this.Given(x => x.A_valid_ingredient_usage_request_is_prepared())
            .When(x => x.The_usage_is_recorded())
            .Then(x => x.The_response_should_contain_the_created_record())
            .BDDfy();
    }

    [Fact]
    public void Listing_usage_by_ingredient_should_return_matching_records()
    {
        this.Given(x => x.An_ingredient_usage_record_exists())
            .When(x => x.The_usage_is_listed_by_ingredient())
            .Then(x => x.The_list_response_should_contain_the_record())
            .BDDfy();
    }

    [Fact]
    public void Getting_usage_summary_should_return_aggregated_data()
    {
        this.Given(x => x.An_ingredient_usage_record_exists())
            .When(x => x.The_summary_is_requested())
            .Then(x => x.The_summary_should_contain_aggregated_data())
            .BDDfy();
    }

    [Fact]
    public void Recording_usage_with_missing_ingredient_name_should_return_bad_request()
    {
        this.Given(x => x.A_usage_request_with_missing_ingredient_name_is_prepared())
            .When(x => x.The_usage_is_recorded())
            .Then(x => x.The_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    [Fact]
    public void Recording_usage_with_zero_quantity_should_return_bad_request()
    {
        this.Given(x => x.A_usage_request_with_zero_quantity_is_prepared())
            .When(x => x.The_usage_is_recorded())
            .Then(x => x.The_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_ingredient_usage_request_is_prepared()
    {
        _ingredientName = $"Flour-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = _ingredientName,
            QuantityUsed = 2.5m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };
        await Task.CompletedTask;
    }

    private async Task The_usage_is_recorded() => await _postSteps.Send();

    private async Task The_response_should_contain_the_created_record()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.IngredientName.Should().Be(_ingredientName);
        _postSteps.Response!.QuantityUsed.Should().Be(2.5m);
        _postSteps.Response!.UsageId.Should().NotBeNullOrEmpty();
    }

    private async Task An_ingredient_usage_record_exists()
    {
        await A_valid_ingredient_usage_request_is_prepared();
        await The_usage_is_recorded();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
    }

    private async Task The_usage_is_listed_by_ingredient() => await _getSteps.RetrieveByIngredient(_ingredientName);

    private async Task The_summary_is_requested() => await _getSteps.RetrieveSummary();

    private async Task The_list_response_should_contain_the_record()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(u => u.IngredientName == _ingredientName);
    }

    private async Task The_summary_should_contain_aggregated_data()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseSummaryResponse();
        _getSteps.SummaryResponse!.Should().Contain(s => s.IngredientName == _ingredientName);
    }

    private async Task A_usage_request_with_missing_ingredient_name_is_prepared()
    {
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = null,
            QuantityUsed = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };
        await Task.CompletedTask;
    }

    private async Task A_usage_request_with_zero_quantity_is_prepared()
    {
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = "Eggs",
            QuantityUsed = 0,
            Unit = "units",
            RecipeName = "Pancakes"
        };
        await Task.CompletedTask;
    }

    private void The_post_response_should_indicate_bad_request()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
