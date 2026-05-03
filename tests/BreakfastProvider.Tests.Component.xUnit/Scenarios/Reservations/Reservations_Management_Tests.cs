using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Reservations;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Reservations;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Reservations;

public class Reservations_Management_Tests : BaseFixture
{
    private readonly PostReservationSteps _postSteps;
    private readonly GetReservationSteps _getSteps;
    private readonly CancelReservationSteps _cancelSteps;

    public Reservations_Management_Tests()
    {
        _postSteps = Get<PostReservationSteps>();
        _getSteps = Get<GetReservationSteps>();
        _cancelSteps = Get<CancelReservationSteps>();
    }

    private TestReservationRequest CreateValidRequest() => new()
    {
        CustomerName = $"Customer-{Guid.NewGuid():N}",
        TableNumber = Random.Shared.Next(1, 50),
        PartySize = Random.Shared.Next(1, 10),
        ReservedAt = DateTime.UtcNow.AddHours(2),
        ContactPhone = "07700900000"
    };

    private async Task<int> CreateReservation()
    {
        _postSteps.Request = CreateValidRequest();
        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        return _postSteps.Response!.Id;
    }

    [Fact]
    [HappyPath]
    public async Task Creating_a_reservation_should_return_the_confirmed_reservation()
    {
        // Given a valid reservation request
        _postSteps.Request = CreateValidRequest();

        // When the reservation is submitted
        await _postSteps.Send();

        // Then the response should contain the confirmed booking
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        Track.That(() => _postSteps.Response!.Status.Should().Be("Confirmed"));
        Track.That(() => _postSteps.Response!.CustomerName.Should().Be(_postSteps.Request.CustomerName));
    }

    [Fact]
    public async Task Retrieving_an_existing_reservation_should_return_the_reservation()
    {
        // Given a reservation exists
        var createdReservationId = await CreateReservation();

        // When the reservation is retrieved by id
        await _getSteps.RetrieveById(createdReservationId);

        // Then the response should contain the reservation
        Track.That(() => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _getSteps.ParseResponse();
        Track.That(() => _getSteps.Response!.Id.Should().Be(createdReservationId));
        Track.That(() => _getSteps.Response!.CustomerName.Should().Be(_postSteps.Response!.CustomerName));
        Track.That(() => _getSteps.Response!.Status.Should().Be("Confirmed"));
    }

    [Fact]
    public async Task Cancelling_a_reservation_should_return_the_cancelled_reservation()
    {
        // Given a reservation exists
        var createdReservationId = await CreateReservation();

        // When the reservation is cancelled
        await _cancelSteps.Send(createdReservationId);

        // Then the cancellation response should indicate the reservation is cancelled
        Track.That(() => _cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _cancelSteps.ParseResponse();
        Track.That(() => _cancelSteps.Response!.Status.Should().Be("Cancelled"));
    }

    [Fact]
    public async Task Cancelling_an_already_cancelled_reservation_should_return_a_conflict_response()
    {
        // Given a cancelled reservation exists
        var createdReservationId = await CreateReservation();
        await _cancelSteps.Send(createdReservationId);
        Track.That(() => _cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

        // When the reservation is cancelled again
        await _cancelSteps.Send(createdReservationId);

        // Then the cancellation response should indicate a conflict
        Track.That(() => _cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Deleting_a_reservation_should_return_no_content()
    {
        // Given a reservation exists
        var createdReservationId = await CreateReservation();

        // When the reservation is deleted
        var request = new HttpRequestMessage(HttpMethod.Delete, $"reservations/{createdReservationId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        var deleteResponse = await Client.SendAsync(request);

        // Then the response should indicate no content
        Track.That(() => deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent));
    }
}
