using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientWaste;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientWaste;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.IngredientWaste;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Ingredient_Waste__Tracking_Feature : BaseFixture
{
    private readonly PostIngredientWasteSteps _postSteps;
    private readonly GetIngredientWasteSteps _getSteps;
    private readonly DeleteIngredientWasteSteps _deleteSteps;
    private string _recipeName = string.Empty;
    private string _createdWasteId = string.Empty;

    public Ingredient_Waste__Tracking_Feature()
    {
        _postSteps = Get<PostIngredientWasteSteps>();
        _getSteps = Get<GetIngredientWasteSteps>();
        _deleteSteps = Get<DeleteIngredientWasteSteps>();
    }

    #region Given

    private async Task A_valid_ingredient_waste_request()
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
    }

    private async Task<CompositeStep> An_ingredient_waste_record_exists()
    {
        return Sub.Steps(
            _ => A_valid_ingredient_waste_request(),
            _ => The_waste_is_recorded(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdWasteId = _postSteps.Response!.WasteId;
    }

    private async Task A_waste_request_with_missing_ingredient_name()
    {
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = null,
            QuantityWasted = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes",
            Reason = "Dropped on floor"
        };
    }

    private async Task A_waste_request_with_zero_quantity()
    {
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = "Eggs",
            QuantityWasted = 0,
            Unit = "units",
            RecipeName = "Pancakes",
            Reason = "Spoiled"
        };
    }

    private async Task A_waste_request_with_missing_reason()
    {
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = "Milk",
            QuantityWasted = 2.0m,
            Unit = "litres",
            RecipeName = "Pancakes",
            Reason = null
        };
    }

    #endregion

    #region When

    private async Task The_waste_is_recorded() => await _postSteps.Send();

    private async Task The_waste_is_listed_by_recipe() => await _getSteps.RetrieveByRecipe(_recipeName);

    private async Task The_waste_record_is_deleted() => await _deleteSteps.Delete(_createdWasteId);

    #endregion

    #region Then

    private async Task The_response_should_contain_the_created_waste_record()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.IngredientName.Should().NotBeNullOrEmpty();
        _postSteps.Response!.QuantityWasted.Should().Be(0.5m);
        _postSteps.Response!.WasteId.Should().NotBeNullOrEmpty();
        _postSteps.Response!.Reason.Should().Be("Expired before use");
    }

    private async Task The_list_response_should_contain_the_waste_record()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(w => w.WasteId == _createdWasteId);
    }

    private async Task The_delete_response_should_indicate_no_content()
        => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    private async Task The_waste_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientWaste;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientWaste;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.IngredientWaste;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Ingredient_Waste__Tracking_Feature : BaseFixture
{
    private readonly PostIngredientWasteSteps _postSteps;
    private readonly GetIngredientWasteSteps _getSteps;
    private readonly DeleteIngredientWasteSteps _deleteSteps;
    private string _recipeName = string.Empty;
    private string _ingredientName = string.Empty;

    public Ingredient_Waste__Tracking_Feature()
    {
        _postSteps = Get<PostIngredientWasteSteps>();
        _getSteps = Get<GetIngredientWasteSteps>();
        _deleteSteps = Get<DeleteIngredientWasteSteps>();
    }

    #region Given

    private async Task A_valid_ingredient_waste_request()
    {
        _ingredientName = $"Butter-{Guid.NewGuid():N}";
        _recipeName = $"Croissants-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = _ingredientName,
            QuantityWasted = 0.75m,
            Unit = "kg",
            RecipeName = _recipeName,
            Reason = "Expired before use"
        };
    }

    private async Task<CompositeStep> An_ingredient_waste_record_exists()
    {
        return Sub.Steps(
            _ => A_valid_ingredient_waste_request(),
            _ => The_waste_is_recorded(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
    }

    private async Task A_waste_request_with_missing_ingredient_name()
    {
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = null,
            QuantityWasted = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes",
            Reason = "Dropped on floor"
        };
    }

    private async Task A_waste_request_with_missing_reason()
    {
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = "Milk",
            QuantityWasted = 2.0m,
            Unit = "litres",
            RecipeName = "Waffles",
            Reason = null
        };
    }

    #endregion

    #region When

    private async Task The_waste_is_recorded() => await _postSteps.Send();

    private async Task The_waste_is_listed_by_recipe() => await _getSteps.RetrieveByRecipe(_recipeName);

    private async Task The_waste_record_is_deleted() => await _deleteSteps.Delete(_postSteps.Response!.WasteId);

    #endregion

    #region Then

    private async Task The_response_should_contain_the_created_waste_record()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.IngredientName.Should().Be(_ingredientName);
        _postSteps.Response!.QuantityWasted.Should().Be(0.75m);
        _postSteps.Response!.Reason.Should().Be("Expired before use");
        _postSteps.Response!.WasteId.Should().NotBeNullOrEmpty();
    }

    private async Task The_list_response_should_contain_the_waste_record()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(w => w.IngredientName == _ingredientName);
    }

    private async Task The_delete_response_should_indicate_no_content()
        => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    private async Task The_waste_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
