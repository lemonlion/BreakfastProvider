using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Feedback;
using BreakfastProvider.Tests.Component.Shared.Models.Feedback;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Feedback;

public class Feedback_Management_Tests : BaseFixture
{
    private readonly PostFeedbackSteps _postSteps;
    private readonly GetFeedbackSteps _getSteps;

    private string _orderId = null!;
    private string _createdFeedbackId = null!;

    public Feedback_Management_Tests()
    {
        _postSteps = Get<PostFeedbackSteps>();
        _getSteps = Get<GetFeedbackSteps>();
    }

    [Fact]
    [HappyPath]
    public void Submitting_feedback_should_return_the_created_feedback()
    {
        this.Given(x => x.A_valid_feedback_request_is_prepared())
            .When(x => x.The_feedback_is_submitted())
            .Then(x => x.The_response_should_contain_the_created_feedback())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_existing_feedback_by_id_should_return_the_feedback()
    {
        this.Given(x => x.A_feedback_entry_exists())
            .When(x => x.The_feedback_is_retrieved_by_id())
            .Then(x => x.The_get_response_should_contain_the_feedback())
            .BDDfy();
    }

    [Fact]
    public void Listing_feedback_for_an_order_should_return_the_feedback()
    {
        this.Given(x => x.A_feedback_entry_exists())
            .When(x => x.The_feedback_is_retrieved_by_order_id())
            .Then(x => x.The_list_response_should_contain_the_feedback())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_non_existent_feedback_should_return_not_found()
    {
        this.When(x => x.A_non_existent_feedback_is_retrieved())
            .Then(x => x.The_get_response_should_indicate_not_found())
            .BDDfy();
    }

    [Fact]
    public void Submitting_feedback_with_missing_customer_name_should_return_bad_request()
    {
        this.Given(x => x.A_feedback_request_with_missing_customer_name_is_prepared())
            .When(x => x.The_feedback_is_submitted())
            .Then(x => x.The_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    [Fact]
    public void Submitting_feedback_with_invalid_rating_should_return_bad_request()
    {
        this.Given(x => x.A_feedback_request_with_an_invalid_rating_is_prepared())
            .When(x => x.The_feedback_is_submitted())
            .Then(x => x.The_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_feedback_request_is_prepared()
    {
        _orderId = Guid.NewGuid().ToString();
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = _orderId,
            Rating = 4,
            Comment = "Great pancakes!"
        };
        await Task.CompletedTask;
    }

    private async Task The_feedback_is_submitted()
    {
        await _postSteps.Send();
    }

    private async Task The_response_should_contain_the_created_feedback()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.CustomerName.Should().Be(_postSteps.Request!.CustomerName);
        _postSteps.Response!.Rating.Should().Be(4);
    }

    private async Task A_feedback_entry_exists()
    {
        _orderId = Guid.NewGuid().ToString();
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = _orderId,
            Rating = 4,
            Comment = "Great pancakes!"
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdFeedbackId = _postSteps.Response!.FeedbackId;
    }

    private async Task The_feedback_is_retrieved_by_id()
    {
        await _getSteps.RetrieveById(_createdFeedbackId);
    }

    private async Task The_get_response_should_contain_the_feedback()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.FeedbackId.Should().Be(_createdFeedbackId);
        _getSteps.Response!.CustomerName.Should().Be(_postSteps.Response!.CustomerName);
        _getSteps.Response!.Rating.Should().Be(4);
    }

    private async Task The_feedback_is_retrieved_by_order_id()
    {
        await _getSteps.RetrieveByOrderId(_orderId);
    }

    private async Task The_list_response_should_contain_the_feedback()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(f => f.FeedbackId == _createdFeedbackId);
    }

    private async Task A_non_existent_feedback_is_retrieved()
    {
        await _getSteps.RetrieveById(Guid.NewGuid().ToString());
    }

    private void The_get_response_should_indicate_not_found()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task A_feedback_request_with_missing_customer_name_is_prepared()
    {
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = null,
            OrderId = Guid.NewGuid().ToString(),
            Rating = 3,
            Comment = "Missing name"
        };
        await Task.CompletedTask;
    }

    private async Task A_feedback_request_with_an_invalid_rating_is_prepared()
    {
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = Guid.NewGuid().ToString(),
            Rating = 0,
            Comment = "Invalid rating"
        };
        await Task.CompletedTask;
    }

    private void The_post_response_should_indicate_bad_request()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
