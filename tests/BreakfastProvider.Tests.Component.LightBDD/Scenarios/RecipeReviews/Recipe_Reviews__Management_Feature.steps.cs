using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeReviews;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeReviews;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.RecipeReviews;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Recipe_Reviews__Management_Feature : BaseFixture
{
    private readonly PostRecipeReviewSteps _postSteps;
    private readonly GetRecipeReviewSteps _getSteps;
    private string _createdReviewId = string.Empty;
    private string _recipeName = string.Empty;

    public Recipe_Reviews__Management_Feature()
    {
        _postSteps = Get<PostRecipeReviewSteps>();
        _getSteps = Get<GetRecipeReviewSteps>();
    }

    #region Given

    private async Task A_valid_recipe_review_request()
    {
        _recipeName = $"Recipe-{Guid.NewGuid():N}";
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = _recipeName,
            ReviewerName = $"Reviewer-{Guid.NewGuid():N}",
            Rating = 5,
            Comments = "Absolutely delicious!",
            Tags = ["fluffy", "breakfast"]
        };
    }

    private async Task<CompositeStep> A_review_entry_exists()
    {
        return Sub.Steps(
            _ => A_valid_recipe_review_request(),
            _ => The_review_is_submitted(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdReviewId = _postSteps.Response!.ReviewId;
    }

    private async Task A_review_request_with_missing_recipe_name()
    {
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = null,
            ReviewerName = "Someone",
            Rating = 3,
            Comments = "Test"
        };
    }

    private async Task A_review_request_with_an_invalid_rating()
    {
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = "Some Recipe",
            ReviewerName = "Someone",
            Rating = 0,
            Comments = "Test"
        };
    }

    #endregion

    #region When

    private async Task The_review_is_submitted() => await _postSteps.Send();

    private async Task The_review_is_retrieved_by_id() => await _getSteps.RetrieveById(_createdReviewId);

    private async Task The_reviews_are_listed_by_recipe() => await _getSteps.RetrieveByRecipe(_recipeName);

    private async Task A_non_existent_review_is_retrieved() => await _getSteps.RetrieveById(Guid.NewGuid().ToString());

    #endregion

    #region Then

    private async Task<CompositeStep> The_review_response_should_contain_the_created_review()
    {
        return Sub.Steps(
            _ => The_post_response_http_status_should_be_created(),
            _ => The_post_response_should_be_valid_json(),
            _ => The_created_review_should_have_the_correct_recipe_name(),
            _ => The_created_review_should_have_the_correct_rating());
    }

    private async Task The_post_response_http_status_should_be_created()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

    private async Task The_post_response_should_be_valid_json()
        => await _postSteps.ParseResponse();

    private async Task The_created_review_should_have_the_correct_recipe_name()
        => _postSteps.Response!.RecipeName.Should().Be(_recipeName);

    private async Task The_created_review_should_have_the_correct_rating()
        => _postSteps.Response!.Rating.Should().Be(5);

    private async Task The_review_get_response_should_contain_the_review()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.ReviewId.Should().Be(_createdReviewId);
        _getSteps.Response!.RecipeName.Should().Be(_recipeName);
    }

    private async Task The_review_list_response_should_contain_the_review()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(r => r.ReviewId == _createdReviewId);
    }

    private async Task The_review_get_response_should_indicate_not_found()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    private async Task The_review_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
