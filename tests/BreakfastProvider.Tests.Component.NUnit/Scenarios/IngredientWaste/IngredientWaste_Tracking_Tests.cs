using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientWaste;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientWaste;
using BreakfastProvider.Tests.Component.NUnit.Infrastructure;
using Kronikol.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.IngredientWaste;

public class IngredientWaste_Tracking_Tests : BaseFixture
{
    private readonly PostIngredientWasteSteps _postSteps;
    private readonly GetIngredientWasteSteps _getSteps;
    private readonly DeleteIngredientWasteSteps _deleteSteps;

    public IngredientWaste_Tracking_Tests()
    {
        _postSteps = Get<PostIngredientWasteSteps>();
        _getSteps = Get<GetIngredientWasteSteps>();
        _deleteSteps = Get<DeleteIngredientWasteSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Recording_ingredient_waste_should_return_the_created_record()
    {
        // Given a valid ingredient waste request
        var recipeName = $"Pancakes-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = $"Butter-{Guid.NewGuid():N}",
            QuantityWasted = 0.5m,
            Unit = "kg",
            RecipeName = recipeName,
            Reason = "Expired before use"
        };

        // When the waste is recorded
        await _postSteps.Send();

        // Then the response should contain the created waste record
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.IngredientName.Should().NotBeNullOrEmpty();
        _postSteps.Response!.QuantityWasted.Should().Be(0.5m);
        _postSteps.Response!.WasteId.Should().NotBeNullOrEmpty();
        _postSteps.Response!.Reason.Should().Be("Expired before use");
    }

    [Test]
    public async Task Listing_waste_by_recipe_should_return_matching_records()
    {
        // Given an ingredient waste record exists
        var recipeName = $"Pancakes-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = $"Butter-{Guid.NewGuid():N}",
            QuantityWasted = 0.5m,
            Unit = "kg",
            RecipeName = recipeName,
            Reason = "Expired before use"
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdWasteId = _postSteps.Response!.WasteId;

        // When the waste is listed by recipe
        await _getSteps.RetrieveByRecipe(recipeName);

        // Then the list should contain the waste record
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(w => w.WasteId == createdWasteId);
    }

    [Test]
    public async Task Deleting_a_waste_record_should_return_no_content()
    {
        // Given an ingredient waste record exists
        var recipeName = $"Pancakes-{Guid.NewGuid():N}";
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = $"Butter-{Guid.NewGuid():N}",
            QuantityWasted = 0.5m,
            Unit = "kg",
            RecipeName = recipeName,
            Reason = "Expired before use"
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdWasteId = _postSteps.Response!.WasteId;

        // When the waste record is deleted
        await _deleteSteps.Delete(createdWasteId);

        // Then the response should indicate no content
        _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Recording_waste_with_missing_ingredient_name_should_return_bad_request()
    {
        // Given a waste request with missing ingredient name
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = null,
            QuantityWasted = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes",
            Reason = "Dropped on floor"
        };

        // When the waste is recorded
        await _postSteps.Send();

        // Then the response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Recording_waste_with_zero_quantity_should_return_bad_request()
    {
        // Given a waste request with zero quantity
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = "Eggs",
            QuantityWasted = 0,
            Unit = "units",
            RecipeName = "Pancakes",
            Reason = "Spoiled"
        };

        // When the waste is recorded
        await _postSteps.Send();

        // Then the response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Recording_waste_with_missing_reason_should_return_bad_request()
    {
        // Given a waste request with missing reason
        _postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = "Milk",
            QuantityWasted = 2.0m,
            Unit = "litres",
            RecipeName = "Pancakes",
            Reason = null
        };

        // When the waste is recorded
        await _postSteps.Send();

        // Then the response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
