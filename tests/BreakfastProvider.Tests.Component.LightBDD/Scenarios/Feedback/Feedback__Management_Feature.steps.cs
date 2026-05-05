using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Feedback;
using BreakfastProvider.Tests.Component.Shared.Models.Feedback;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Feedback;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Feedback__Management_Feature : BaseFixture
{
    private readonly PostFeedbackSteps _postSteps;
    private readonly GetFeedbackSteps _getSteps;
    private string _createdFeedbackId = string.Empty;
    private string _orderId = string.Empty;

    public Feedback__Management_Feature()
    {
        _postSteps = Get<PostFeedbackSteps>();
        _getSteps = Get<GetFeedbackSteps>();
    }

    #region Given

    private async Task A_valid_feedback_request()
    {
        _orderId = Guid.NewGuid().ToString();
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = _orderId,
            Rating = 4,
            Comment = "Great pancakes!"
        };
    }

    private async Task<CompositeStep> A_feedback_entry_exists()
    {
        return Sub.Steps(
            _ => A_valid_feedback_request(),
            _ => The_feedback_is_submitted(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdFeedbackId = _postSteps.Response!.FeedbackId;
    }

    private async Task A_feedback_request_with_missing_customer_name()
    {
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = null,
            OrderId = Guid.NewGuid().ToString(),
            Rating = 3,
            Comment = "Missing name"
        };
    }

    private async Task A_feedback_request_with_an_invalid_rating()
    {
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = Guid.NewGuid().ToString(),
            Rating = 0,
            Comment = "Invalid rating"
        };
    }

    #endregion

    #region When

    private async Task The_feedback_is_submitted()
        => await _postSteps.Send();

    private async Task The_feedback_is_retrieved_by_id()
        => await _getSteps.RetrieveById(_createdFeedbackId);

    private async Task The_feedback_is_retrieved_by_order_id()
        => await _getSteps.RetrieveByOrderId(_orderId);

    private async Task A_non_existent_feedback_is_retrieved()
        => await _getSteps.RetrieveById(Guid.NewGuid().ToString());

    #endregion

    #region Then

    private async Task<CompositeStep> The_feedback_response_should_contain_the_created_feedback()
    {
        return Sub.Steps(
            _ => The_post_response_http_status_should_be_created(),
            _ => The_post_response_should_be_valid_json(),
            _ => The_created_feedback_should_have_the_correct_customer_name(),
            _ => The_created_feedback_should_have_the_correct_rating());
    }

    private async Task The_post_response_http_status_should_be_created()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

    private async Task The_post_response_should_be_valid_json()
        => await _postSteps.ParseResponse();

    private async Task The_created_feedback_should_have_the_correct_customer_name()
        => _postSteps.Response!.CustomerName.Should().Be(_postSteps.Request.CustomerName);

    private async Task The_created_feedback_should_have_the_correct_rating()
        => _postSteps.Response!.Rating.Should().Be(4);

    private async Task<CompositeStep> The_feedback_get_response_should_contain_the_feedback()
    {
        return Sub.Steps(
            _ => The_get_response_http_status_should_be_ok(),
            _ => The_get_response_should_be_valid_json(),
            _ => The_retrieved_feedback_should_match_the_created_feedback());
    }

    private async Task The_get_response_http_status_should_be_ok()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_get_response_should_be_valid_json()
        => await _getSteps.ParseResponse();

    private async Task The_retrieved_feedback_should_match_the_created_feedback()
    {
        _getSteps.Response!.FeedbackId.Should().Be(_createdFeedbackId);
        _getSteps.Response!.CustomerName.Should().Be(_postSteps.Response!.CustomerName);
        _getSteps.Response!.Rating.Should().Be(4);
    }

    private async Task<CompositeStep> The_feedback_list_response_should_contain_the_feedback()
    {
        return Sub.Steps(
            _ => The_list_response_http_status_should_be_ok(),
            _ => The_list_response_should_be_valid_json(),
            _ => The_list_should_contain_the_created_feedback());
    }

    private async Task The_list_response_http_status_should_be_ok()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_list_response_should_be_valid_json()
        => await _getSteps.ParseListResponse();

    private async Task The_list_should_contain_the_created_feedback()
        => _getSteps.ListResponse!.Should().Contain(f => f.FeedbackId == _createdFeedbackId);

    private async Task The_feedback_get_response_should_indicate_not_found()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    private async Task The_feedback_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
