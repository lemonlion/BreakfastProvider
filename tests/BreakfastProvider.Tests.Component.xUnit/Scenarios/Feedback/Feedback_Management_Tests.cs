using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Feedback;
using BreakfastProvider.Tests.Component.Shared.Models.Feedback;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Feedback;

public class Feedback_Management_Tests : BaseFixture
{
    private readonly PostFeedbackSteps _postSteps;
    private readonly GetFeedbackSteps _getSteps;

    public Feedback_Management_Tests()
    {
        _postSteps = Get<PostFeedbackSteps>();
        _getSteps = Get<GetFeedbackSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Submitting_feedback_should_return_the_created_feedback()
    {
        // Given a valid feedback request
        var orderId = Guid.NewGuid().ToString();
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = orderId,
            Rating = 4,
            Comment = "Great pancakes!"
        };

        // When the feedback is submitted
        await _postSteps.Send();

        // Then the response should contain the created feedback
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        Track.That(() => _postSteps.Response!.CustomerName.Should().Be(_postSteps.Request.CustomerName));
        Track.That(() => _postSteps.Response!.Rating.Should().Be(4));
    }

    [Fact]
    public async Task Retrieving_existing_feedback_by_id_should_return_the_feedback()
    {
        // Given a feedback entry exists
        var orderId = Guid.NewGuid().ToString();
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = orderId,
            Rating = 4,
            Comment = "Great pancakes!"
        };
        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        var createdFeedbackId = _postSteps.Response!.FeedbackId;

        // When the feedback is retrieved by id
        await _getSteps.RetrieveById(createdFeedbackId);

        // Then the response should contain the feedback
        Track.That(() => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _getSteps.ParseResponse();
        Track.That(() => _getSteps.Response!.FeedbackId.Should().Be(createdFeedbackId));
        Track.That(() => _getSteps.Response!.CustomerName.Should().Be(_postSteps.Response!.CustomerName));
        Track.That(() => _getSteps.Response!.Rating.Should().Be(4));
    }

    [Fact]
    public async Task Listing_feedback_for_an_order_should_return_the_feedback()
    {
        // Given a feedback entry exists
        var orderId = Guid.NewGuid().ToString();
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = orderId,
            Rating = 4,
            Comment = "Great pancakes!"
        };
        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        var createdFeedbackId = _postSteps.Response!.FeedbackId;

        // When the feedback is retrieved by order id
        await _getSteps.RetrieveByOrderId(orderId);

        // Then the list response should contain the feedback
        Track.That(() => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _getSteps.ParseListResponse();
        Track.That(() => _getSteps.ListResponse!.Should().Contain(f => f.FeedbackId == createdFeedbackId));
    }

    [Fact]
    public async Task Retrieving_non_existent_feedback_should_return_not_found()
    {
        // When a non-existent feedback is retrieved
        await _getSteps.RetrieveById(Guid.NewGuid().ToString());

        // Then the response should indicate not found
        Track.That(() => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task Submitting_feedback_with_missing_customer_name_should_return_bad_request()
    {
        // Given a feedback request with missing customer name
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = null,
            OrderId = Guid.NewGuid().ToString(),
            Rating = 3,
            Comment = "Missing name"
        };

        // When the feedback is submitted
        await _postSteps.Send();

        // Then the response should indicate bad request
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task Submitting_feedback_with_invalid_rating_should_return_bad_request()
    {
        // Given a feedback request with an invalid rating
        _postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = Guid.NewGuid().ToString(),
            Rating = 0,
            Comment = "Invalid rating"
        };

        // When the feedback is submitted
        await _postSteps.Send();

        // Then the response should indicate bad request
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
    }
}
