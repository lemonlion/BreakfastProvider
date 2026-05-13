using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientUsage;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientUsage;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.IngredientUsage;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Ingredient_Usage__Analytics_Feature : BaseFixture
{
    private readonly PostIngredientUsageSteps _postSteps;
    private readonly GetIngredientUsageSteps _getSteps;
    private string _ingredientName = string.Empty;

    public Ingredient_Usage__Analytics_Feature()
    {
        _postSteps = Get<PostIngredientUsageSteps>();
        _getSteps = Get<GetIngredientUsageSteps>();
    }

    #region Given

    private async Task A_valid_ingredient_usage_request()
    {
        _ingredientName = $"Flour-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = _ingredientName,
            QuantityUsed = 2.5m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };
    }

    private async Task<CompositeStep> An_ingredient_usage_record_exists()
    {
        return Sub.Steps(
            _ => A_valid_ingredient_usage_request(),
            _ => The_usage_is_recorded(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
    }

    private async Task A_usage_request_with_missing_ingredient_name()
    {
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = null,
            QuantityUsed = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes"
        };
    }

    private async Task A_usage_request_with_zero_quantity()
    {
        _postSteps.Request = new TestIngredientUsageRequest
        {
            IngredientName = "Eggs",
            QuantityUsed = 0,
            Unit = "units",
            RecipeName = "Pancakes"
        };
    }

    #endregion

    #region When

    private async Task The_usage_is_recorded() => await _postSteps.Send();

    private async Task The_usage_is_listed_by_ingredient() => await _getSteps.RetrieveByIngredient(_ingredientName);

    private async Task The_summary_is_requested() => await _getSteps.RetrieveSummary();

    #endregion

    #region Then

    private async Task The_response_should_contain_the_created_record()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.IngredientName.Should().Be(_ingredientName);
        _postSteps.Response!.QuantityUsed.Should().Be(2.5m);
        _postSteps.Response!.UsageId.Should().NotBeNullOrEmpty();
    }

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

    private async Task The_usage_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
