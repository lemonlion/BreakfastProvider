using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeReviews;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeReviews;
using BreakfastProvider.Tests.Component.NUnit.Infrastructure;
using TestTrackingDiagrams.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.RecipeReviews;

public class RecipeReview_Management_Tests : BaseFixture
{
    private readonly PostRecipeReviewSteps _postSteps;
    private readonly GetRecipeReviewSteps _getSteps;

    public RecipeReview_Management_Tests()
    {
        _postSteps = Get<PostRecipeReviewSteps>();
        _getSteps = Get<GetRecipeReviewSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Submitting_a_recipe_review_should_return_the_created_review()
    {
        // Given a valid recipe review request
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = "Fluffy Pancakes",
            ReviewerName = $"Reviewer-{Guid.NewGuid():N}",
            Rating = 5,
            Comments = "Absolutely delicious!",
            Tags = ["fluffy", "breakfast"]
        };

        // When the review is submitted
        await _postSteps.Send();

        // Then the response should contain the created review
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.RecipeName.Should().Be("Fluffy Pancakes");
        _postSteps.Response!.Rating.Should().Be(5);
        _postSteps.Response!.ReviewId.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Retrieving_existing_review_by_id_should_return_the_review()
    {
        // Given a review exists
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = "Classic Waffles",
            ReviewerName = $"Reviewer-{Guid.NewGuid():N}",
            Rating = 4,
            Comments = "Crispy and golden!",
            Tags = ["crispy"]
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdReviewId = _postSteps.Response!.ReviewId;

        // When the review is retrieved by id
        await _getSteps.RetrieveById(createdReviewId);

        // Then the response should contain the review
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.ReviewId.Should().Be(createdReviewId);
        _getSteps.Response!.RecipeName.Should().Be("Classic Waffles");
        _getSteps.Response!.Rating.Should().Be(4);
    }

    [Test]
    public async Task Listing_reviews_by_recipe_should_return_matching_reviews()
    {
        // Given a review exists for a specific recipe
        var recipeName = $"Recipe-{Guid.NewGuid():N}";
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = recipeName,
            ReviewerName = $"Reviewer-{Guid.NewGuid():N}",
            Rating = 3,
            Comments = "It was okay.",
            Tags = ["average"]
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdReviewId = _postSteps.Response!.ReviewId;

        // When reviews are listed by recipe
        await _getSteps.RetrieveByRecipe(recipeName);

        // Then the list should contain the created review
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(r => r.ReviewId == createdReviewId);
    }

    [Test]
    public async Task Retrieving_non_existent_review_should_return_not_found()
    {
        // When a non-existent review is retrieved
        await _getSteps.RetrieveById(Guid.NewGuid().ToString());

        // Then the response should be not found
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Submitting_review_with_missing_recipe_name_should_return_bad_request()
    {
        // Given an invalid request with missing recipe name
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = null,
            ReviewerName = "Someone",
            Rating = 3,
            Comments = "Test"
        };

        // When the review is submitted
        await _postSteps.Send();

        // Then the response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Submitting_review_with_invalid_rating_should_return_bad_request()
    {
        // Given an invalid request with rating out of range
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = "Some Recipe",
            ReviewerName = "Someone",
            Rating = 0,
            Comments = "Test"
        };

        // When the review is submitted
        await _postSteps.Send();

        // Then the response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

