using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeReviews;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeReviews;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.RecipeReviews;

public class RecipeReview_Management_Tests : BaseFixture
{
    private readonly PostRecipeReviewSteps _postSteps;
    private readonly GetRecipeReviewSteps _getSteps;

    private string _recipeName = null!;
    private string _createdReviewId = null!;

    public RecipeReview_Management_Tests()
    {
        _postSteps = Get<PostRecipeReviewSteps>();
        _getSteps = Get<GetRecipeReviewSteps>();
    }

    [Fact]
    [HappyPath]
    public void Submitting_a_recipe_review_should_return_the_created_review()
    {
        this.Given(x => x.A_valid_recipe_review_request_is_prepared())
            .When(x => x.The_review_is_submitted())
            .Then(x => x.The_response_should_contain_the_created_review())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_existing_review_by_id_should_return_the_review()
    {
        this.Given(x => x.A_review_entry_exists())
            .When(x => x.The_review_is_retrieved_by_id())
            .Then(x => x.The_get_response_should_contain_the_review())
            .BDDfy();
    }

    [Fact]
    public void Listing_reviews_by_recipe_should_return_matching_reviews()
    {
        this.Given(x => x.A_review_entry_exists())
            .When(x => x.The_reviews_are_listed_by_recipe())
            .Then(x => x.The_list_response_should_contain_the_review())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_non_existent_review_should_return_not_found()
    {
        this.When(x => x.A_non_existent_review_is_retrieved())
            .Then(x => x.The_get_response_should_indicate_not_found())
            .BDDfy();
    }

    [Fact]
    public void Submitting_review_with_missing_recipe_name_should_return_bad_request()
    {
        this.Given(x => x.A_review_request_with_missing_recipe_name_is_prepared())
            .When(x => x.The_review_is_submitted())
            .Then(x => x.The_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    [Fact]
    public void Submitting_review_with_invalid_rating_should_return_bad_request()
    {
        this.Given(x => x.A_review_request_with_an_invalid_rating_is_prepared())
            .When(x => x.The_review_is_submitted())
            .Then(x => x.The_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_recipe_review_request_is_prepared()
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
        await Task.CompletedTask;
    }

    private async Task The_review_is_submitted() => await _postSteps.Send();

    private async Task The_response_should_contain_the_created_review()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.RecipeName.Should().Be(_recipeName);
        _postSteps.Response!.Rating.Should().Be(5);
        _postSteps.Response!.ReviewId.Should().NotBeNullOrEmpty();
    }

    private async Task A_review_entry_exists()
    {
        await A_valid_recipe_review_request_is_prepared();
        await The_review_is_submitted();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdReviewId = _postSteps.Response!.ReviewId;
    }

    private async Task The_review_is_retrieved_by_id() => await _getSteps.RetrieveById(_createdReviewId);

    private async Task The_reviews_are_listed_by_recipe() => await _getSteps.RetrieveByRecipe(_recipeName);

    private async Task A_non_existent_review_is_retrieved() => await _getSteps.RetrieveById(Guid.NewGuid().ToString());

    private async Task The_get_response_should_contain_the_review()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.ReviewId.Should().Be(_createdReviewId);
        _getSteps.Response!.RecipeName.Should().Be(_recipeName);
    }

    private async Task The_list_response_should_contain_the_review()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(r => r.ReviewId == _createdReviewId);
    }

    private void The_get_response_should_indicate_not_found()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task A_review_request_with_missing_recipe_name_is_prepared()
    {
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = null,
            ReviewerName = "Someone",
            Rating = 3,
            Comments = "Test"
        };
        await Task.CompletedTask;
    }

    private async Task A_review_request_with_an_invalid_rating_is_prepared()
    {
        _postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = "Some Recipe",
            ReviewerName = "Someone",
            Rating = 0,
            Comments = "Test"
        };
        await Task.CompletedTask;
    }

    private void The_post_response_should_indicate_bad_request()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
