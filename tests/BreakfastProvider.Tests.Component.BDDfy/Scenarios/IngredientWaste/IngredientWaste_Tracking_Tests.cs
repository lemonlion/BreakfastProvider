using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientWaste;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientWaste;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.IngredientWaste;

public class IngredientWaste_Tracking_Tests : BaseFixture
{
    private readonly PostIngredientWasteSteps _postSteps;
    private readonly GetIngredientWasteSteps _getSteps;
    private readonly DeleteIngredientWasteSteps _deleteSteps;

    private string _recipeName = null!;
    private string _createdWasteId = null!;

    public IngredientWaste_Tracking_Tests()
    {
        _postSteps = Get<PostIngredientWasteSteps>();
        _getSteps = Get<GetIngredientWasteSteps>();
        _deleteSteps = Get<DeleteIngredientWasteSteps>();
    }

    [Fact]
    [HappyPath]
    public void Recording_ingredient_waste_should_return_the_created_record()
    {
        this.Given(x => x.A_valid_ingredient_waste_request_is_prepared())
            .When(x => x.The_waste_is_recorded())
            .Then(x => x.The_response_should_contain_the_created_waste_record())
            .BDDfy();
    }

    [Fact]
    public void Listing_waste_by_recipe_should_return_matching_records()
    {
        this.Given(x => x.An_ingredient_waste_record_exists())
            .When(x => x.The_waste_is_listed_by_recipe())
            .Then(x => x.The_list_response_should_contain_the_waste_record())
            .BDDfy();
    }

    [Fact]
    public void Deleting_a_waste_record_should_return_no_content()
    {
        this.Given(x => x.An_ingredient_waste_record_exists())
            .When(x => x.The_waste_record_is_deleted())
            .Then(x => x.The_delete_response_should_indicate_no_content())
            .BDDfy();
    }

    [Fact]
    public void Recording_waste_with_missing_ingredient_name_should_return_bad_request()
    {
        this.Given(x => x.A_waste_request_with_missing_ingredient_name_is_prepared())
            .When(x => x.The_waste_is_recorded())
            .Then(x => x.The_waste_response_should_indicate_bad_request())
            .BDDfy();
    }

    [Fact]
    public void Recording_waste_with_zero_quantity_should_return_bad_request()
    {
        this.Given(x => x.A_waste_request_with_zero_quantity_is_prepared())
            .When(x => x.The_waste_is_recorded())
            .Then(x => x.The_waste_response_should_indicate_bad_request())
            .BDDfy();
    }

    [Fact]
    public void Recording_waste_with_missing_reason_should_return_bad_request()
    {
        this.Given(x => x.A_waste_request_with_missing_reason_is_prepared())
            .When(x => x.The_waste_is_recorded())
            .Then(x => x.The_waste_response_should_indicate_bad_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_ingredient_waste_request_is_prepared()
    {
        _recipeName = $"Pancakes-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = $"Butter-{Guid.NewGuid():N}",
            QuantityWasted = 0.5m,
            Unit = "kg",
            RecipeName = _recipeName,
            Reason = "Expired before use"
        };
        await Task.CompletedTask;
    }

    private async Task The_waste_is_recorded() => await _postSteps.Send();

    private async Task The_response_should_contain_the_created_waste_record()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.IngredientName.Should().NotBeNullOrEmpty();
        _postSteps.Response!.QuantityWasted.Should().Be(0.5m);
        _postSteps.Response!.WasteId.Should().NotBeNullOrEmpty();
        _postSteps.Response!.Reason.Should().Be("Expired before use");
    }

    private async Task An_ingredient_waste_record_exists()
    {
        await A_valid_ingredient_waste_request_is_prepared();
        await The_waste_is_recorded();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdWasteId = _postSteps.Response!.WasteId;
    }

    private async Task The_waste_is_listed_by_recipe() => await _getSteps.RetrieveByRecipe(_recipeName);

    private async Task The_list_response_should_contain_the_waste_record()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(w => w.WasteId == _createdWasteId);
    }

    private async Task The_waste_record_is_deleted() => await _deleteSteps.Delete(_createdWasteId);

    private void The_delete_response_should_indicate_no_content()
        => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    private async Task A_waste_request_with_missing_ingredient_name_is_prepared()
    {
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = null,
            QuantityWasted = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes",
            Reason = "Dropped on floor"
        };
        await Task.CompletedTask;
    }

    private async Task A_waste_request_with_zero_quantity_is_prepared()
    {
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = "Eggs",
            QuantityWasted = 0,
            Unit = "units",
            RecipeName = "Pancakes",
            Reason = "Spoiled"
        };
        await Task.CompletedTask;
    }

    private async Task A_waste_request_with_missing_reason_is_prepared()
    {
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = "Milk",
            QuantityWasted = 2.0m,
            Unit = "litres",
            RecipeName = "Pancakes",
            Reason = null
        };
        await Task.CompletedTask;
    }

    private void The_waste_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
