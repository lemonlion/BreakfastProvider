using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Reservations;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Reservations;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Reservations;

[Binding]
public class ReservationsManagementSteps(
    AppManager appManager,
    PostReservationSteps postSteps,
    GetReservationSteps getSteps,
    CancelReservationSteps cancelSteps)
{
    private int _createdReservationId;
    private HttpResponseMessage? _deleteResponse;

    [Given("a valid reservation request")]
    public void GivenAValidReservationRequest()
    {
        postSteps.Request = new TestReservationRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            TableNumber = Random.Shared.Next(1, 50),
            PartySize = Random.Shared.Next(1, 10),
            ReservedAt = DateTime.UtcNow.AddHours(2),
            ContactPhone = "07700900000"
        };
    }

    [Given("a reservation exists")]
    public async Task GivenAReservationExists()
    {
        GivenAValidReservationRequest();
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _createdReservationId = postSteps.Response!.Id;
    }

    [Given("a cancelled reservation exists")]
    public async Task GivenACancelledReservationExists()
    {
        await GivenAReservationExists();
        await cancelSteps.Send(_createdReservationId);
        cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [When("the reservation is submitted")]
    public async Task WhenTheReservationIsSubmitted() => await postSteps.Send();

    [When("the reservation is retrieved by id")]
    public async Task WhenTheReservationIsRetrievedById() => await getSteps.RetrieveById(_createdReservationId);

    [When("the reservation is cancelled")]
    public async Task WhenTheReservationIsCancelled() => await cancelSteps.Send(_createdReservationId);

    [When("the reservation is cancelled again")]
    public async Task WhenTheReservationIsCancelledAgain() => await cancelSteps.Send(_createdReservationId);

    [When("the reservation is deleted")]
    public async Task WhenTheReservationIsDeleted()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"reservations/{_createdReservationId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        _deleteResponse = await appManager.Client.SendAsync(request);
    }

    [Then("the reservation response should contain the confirmed booking")]
    public async Task ThenTheReservationResponseShouldContainTheConfirmedBooking()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.Status.Should().Be("Confirmed");
        postSteps.Response!.CustomerName.Should().Be(postSteps.Request.CustomerName);
    }

    [Then("the reservation get response should contain the reservation")]
    public async Task ThenTheReservationGetResponseShouldContainTheReservation()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseResponse();
        getSteps.Response!.Id.Should().Be(_createdReservationId);
        getSteps.Response!.CustomerName.Should().Be(postSteps.Response!.CustomerName);
        getSteps.Response!.Status.Should().Be("Confirmed");
    }

    [Then("the cancellation response should indicate the reservation is cancelled")]
    public async Task ThenTheCancellationResponseShouldIndicateTheReservationIsCancelled()
    {
        cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await cancelSteps.ParseResponse();
        cancelSteps.Response!.Status.Should().Be("Cancelled");
    }

    [Then("the cancellation response should indicate a conflict")]
    public void ThenTheCancellationResponseShouldIndicateAConflict()
        => cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict);

    [Then("the reservation delete response should indicate no content")]
    public void ThenTheReservationDeleteResponseShouldIndicateNoContent()
        => _deleteResponse!.StatusCode.Should().Be(HttpStatusCode.NoContent);
}
