using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Feedback;
using BreakfastProvider.Tests.Component.Shared.Models.Feedback;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Feedback;

[Binding]
public class FeedbackManagementSteps(
    PostFeedbackSteps postSteps,
    GetFeedbackSteps getSteps)
{
    private string _createdFeedbackId = string.Empty;
    private string _orderId = string.Empty;

    [Given("a valid feedback request")]
    public void GivenAValidFeedbackRequest()
    {
        _orderId = Guid.NewGuid().ToString();
        postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = _orderId,
            Rating = 4,
            Comment = "Great pancakes!"
        };
    }

    [Given("a feedback entry exists")]
    public async Task GivenAFeedbackEntryExists()
    {
        GivenAValidFeedbackRequest();
        await postSteps.Send();
        Track.That(() => postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await postSteps.ParseResponse();
        _createdFeedbackId = postSteps.Response!.FeedbackId;
    }

    [Given("a feedback request with missing customer name")]
    public void GivenAFeedbackRequestWithMissingCustomerName()
    {
        postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = null,
            OrderId = Guid.NewGuid().ToString(),
            Rating = 3,
            Comment = "Missing name"
        };
    }

    [Given("a feedback request with an invalid rating")]
    public void GivenAFeedbackRequestWithAnInvalidRating()
    {
        postSteps.Request = new TestFeedbackRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            OrderId = Guid.NewGuid().ToString(),
            Rating = 0,
            Comment = "Invalid rating"
        };
    }

    [When("the feedback is submitted")]
    public async Task WhenTheFeedbackIsSubmitted() => await postSteps.Send();

    [When("the feedback is retrieved by id")]
    public async Task WhenTheFeedbackIsRetrievedById() => await getSteps.RetrieveById(_createdFeedbackId);

    [When("the feedback is retrieved by order id")]
    public async Task WhenTheFeedbackIsRetrievedByOrderId() => await getSteps.RetrieveByOrderId(_orderId);

    [When("a non-existent feedback is retrieved")]
    public async Task WhenANonExistentFeedbackIsRetrieved() => await getSteps.RetrieveById(Guid.NewGuid().ToString());

    [Then("the feedback response should contain the created feedback")]
    public async Task ThenTheFeedbackResponseShouldContainTheCreatedFeedback()
    {
        Track.That(() => postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await postSteps.ParseResponse();
        Track.That(() => postSteps.Response!.CustomerName.Should().Be(postSteps.Request.CustomerName));
        Track.That(() => postSteps.Response!.Rating.Should().Be(4));
    }

    [Then("the feedback get response should contain the feedback")]
    public async Task ThenTheFeedbackGetResponseShouldContainTheFeedback()
    {
        Track.That(() => getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await getSteps.ParseResponse();
        Track.That(() => getSteps.Response!.FeedbackId.Should().Be(_createdFeedbackId));
        Track.That(() => getSteps.Response!.CustomerName.Should().Be(postSteps.Response!.CustomerName));
        Track.That(() => getSteps.Response!.Rating.Should().Be(4));
    }

    [Then("the feedback list response should contain the feedback")]
    public async Task ThenTheFeedbackListResponseShouldContainTheFeedback()
    {
        Track.That(() => getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await getSteps.ParseListResponse();
        Track.That(() => getSteps.ListResponse!.Should().Contain(f => f.FeedbackId == _createdFeedbackId));
    }

    [Then("the feedback get response should indicate not found")]
    public void ThenTheFeedbackGetResponseShouldIndicateNotFound()
        => Track.That(() => getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));

    [Then("the feedback response should indicate bad request")]
    public void ThenTheFeedbackResponseShouldIndicateBadRequest()
        => Track.That(() => postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
}
