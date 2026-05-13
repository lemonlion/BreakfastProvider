using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.RecipeReviews;
using BreakfastProvider.Tests.Component.Shared.Models.RecipeReviews;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.RecipeReviews;

[Binding]
public class RecipeReviewManagementSteps(
    AppManager appManager,
    PostRecipeReviewSteps postSteps,
    GetRecipeReviewSteps getSteps)
{
    private string _recipeName = string.Empty;
    private string _createdReviewId = string.Empty;

    [Given("a valid recipe review request")]
    public void GivenAValidRecipeReviewRequest()
    {
        _recipeName = $"Recipe-{Guid.NewGuid():N}";
        postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = _recipeName,
            ReviewerName = $"Reviewer-{Guid.NewGuid():N}",
            Rating = 5,
            Comments = "Absolutely delicious!",
            Tags = ["fluffy", "breakfast"]
        };
    }

    [Given("a recipe review has been created")]
    public async Task GivenARecipeReviewHasBeenCreated()
    {
        GivenAValidRecipeReviewRequest();
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _createdReviewId = postSteps.Response!.ReviewId;
    }

    [Given("a recipe review request with a missing recipe name")]
    public void GivenARecipeReviewRequestWithAMissingRecipeName()
    {
        postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = null,
            ReviewerName = "Someone",
            Rating = 3,
            Comments = "Test"
        };
    }

    [Given("a recipe review request with an invalid rating")]
    public void GivenARecipeReviewRequestWithAnInvalidRating()
    {
        postSteps.Request = new TestRecipeReviewRequest
        {
            RecipeName = "Some Recipe",
            ReviewerName = "Someone",
            Rating = 0,
            Comments = "Test"
        };
    }

    [When("the recipe review is submitted")]
    public async Task WhenTheRecipeReviewIsSubmitted()
    {
        await postSteps.Send();
    }

    [When("the review is retrieved by id")]
    public async Task WhenTheReviewIsRetrievedById()
    {
        await getSteps.RetrieveById(_createdReviewId);
    }

    [When("the reviews are listed by recipe name")]
    public async Task WhenTheReviewsAreListedByRecipeName()
    {
        await getSteps.RetrieveByRecipe(_recipeName);
    }

    [When("a non-existent review is retrieved")]
    public async Task WhenANonExistentReviewIsRetrieved()
    {
        await getSteps.RetrieveById(Guid.NewGuid().ToString());
    }

    [Then("the recipe review response should contain the created review")]
    public async Task ThenTheRecipeReviewResponseShouldContainTheCreatedReview()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.RecipeName.Should().Be(_recipeName);
        postSteps.Response!.Rating.Should().Be(5);
        postSteps.Response!.ReviewId.Should().NotBeNullOrEmpty();
    }

    [Then("the get response should contain the review")]
    public async Task ThenTheGetResponseShouldContainTheReview()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseResponse();
        getSteps.Response!.ReviewId.Should().Be(_createdReviewId);
        getSteps.Response!.RecipeName.Should().Be(_recipeName);
    }

    [Then("the list response should contain the review")]
    public async Task ThenTheListResponseShouldContainTheReview()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseListResponse();
        getSteps.ListResponse!.Should().Contain(r => r.ReviewId == _createdReviewId);
    }

    [Then("the review get response should indicate not found")]
    public void ThenTheReviewGetResponseShouldIndicateNotFound()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Then("the review post response should indicate bad request")]
    public void ThenTheReviewPostResponseShouldIndicateBadRequest()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
