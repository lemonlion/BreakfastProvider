using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeReviews;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeReviews;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.RecipeReviews;

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
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        await _postSteps.Response!.RecipeName.Should().BeEqualTo("Fluffy Pancakes");
        await _postSteps.Response!.Rating.Should().BeEqualTo(5);
        await _postSteps.Response!.ReviewId.Should().NotBeNull();
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
            Comments = "Great texture!"
        };
        await _postSteps.Send();
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var reviewId = _postSteps.Response!.ReviewId;

        // When the review is retrieved by id
        await _getSteps.RetrieveById(reviewId);

        // Then the response should contain the review
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        await _getSteps.Response!.ReviewId.Should().BeEqualTo(reviewId);
        await _getSteps.Response!.RecipeName.Should().BeEqualTo("Classic Waffles");
    }

    [Test]
    public async Task Listing_reviews_by_recipe_should_return_matching_reviews()
    {
        // Given a review exists
        var recipeName = $"Recipe-{Guid.NewGuid():N}";
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = recipeName,
            ReviewerName = $"Reviewer-{Guid.NewGuid():N}",
            Rating = 3,
            Comments = "Decent"
        };
        await _postSteps.Send();
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var reviewId = _postSteps.Response!.ReviewId;

        // When the reviews are listed by recipe
        await _getSteps.RetrieveByRecipe(recipeName);

        // Then the list should contain the review
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        await _getSteps.ListResponse!.Should().Contain(r => r.ReviewId == reviewId);
    }

    [Test]
    public async Task Retrieving_non_existent_review_should_return_not_found()
    {
        // When retrieving a non-existent review
        await _getSteps.RetrieveById(Guid.NewGuid().ToString());

        // Then the response should be 404
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Submitting_review_with_missing_recipe_name_should_return_bad_request()
    {
        // Given a request with missing recipe name
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = null,
            ReviewerName = "Someone",
            Rating = 3,
            Comments = "Test"
        };

        // When submitted
        await _postSteps.Send();

        // Then bad request
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Submitting_review_with_invalid_rating_should_return_bad_request()
    {
        // Given a request with an invalid rating
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = "Some Recipe",
            ReviewerName = "Someone",
            Rating = 0,
            Comments = "Test"
        };

        // When submitted
        await _postSteps.Send();

        // Then bad request
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.BadRequest);
    }
}
